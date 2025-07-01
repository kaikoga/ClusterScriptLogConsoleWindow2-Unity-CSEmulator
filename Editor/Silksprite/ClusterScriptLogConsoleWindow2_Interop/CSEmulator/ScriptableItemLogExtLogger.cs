using System;
using System.Linq;
using Assets.KaomoLab.CSEmulator;
using Assets.KaomoLab.CSEmulator.Components;
using ClusterVR.CreatorKit.Item;
using ClusterVR.CreatorKit.Item.Implements;
using Jint.Native;
using Silksprite.ClusterScriptLogConsoleWindow2.Format;
using UnityEngine;
using ILogger = Assets.KaomoLab.CSEmulator.Editor.ILogger;

namespace Silksprite.ClusterScriptLogConsoleWindow2.Interop.CSEmulator
{
    public class ScriptableItemLogExtLogger : ILogger
    {
        readonly IPlayerMeta _playerMeta;
        readonly IProgramStatus _programStatus;
        readonly Item _item;

        public ScriptableItemLogExtLogger(GameObject gameObject, IPlayerMeta playerMeta, IProgramStatus programStatus)
        {
            _playerMeta = playerMeta;
            _programStatus = programStatus;
            _item = gameObject.GetComponent<Item>();
        }

        void Log(string type, string message)
        {
            var (itemId, itemName) = _item != null ? (_item.Id.Value, ((IItem)_item).ItemName) : (0L, "");
            var (userId, userName) = _playerMeta != null ? (_playerMeta.userId, _playerMeta.userDisplayName) : ("", "");

            ScriptableItemLogExtWriter.Enqueue(new OutputScriptableItemLogExt
            {
                // ReSharper disable once PossibleLossOfFraction
                tsdv = (DateTimeOffset.Now - DateTimeOffset.UnixEpoch).Ticks / 10_000_000d,
                dvid = "Editor",
                origin =
                {
                    id = itemId,
                    name = itemName
                },
                player = {
                    id = userId,
                    userName = userName
                },
                type = type,
                message = message,
                kind = GuessKind(message),
                pos = ParseProgramPosition(),
                stack = ParseProgramStack()
            });
        }

        public void Info(string message)
        {
            Log("PreviewLog_Information", message);
        }

        public void Warning(string message)
        {
            Log("PreviewLog_Warning", message);
        }

        public void Error(string message)
        {
            Log("PreviewLog_Error", message);
        }

        public void Exception(JsError e)
        {
            var ps = e.GetOwnProperties()
                .ToDictionary(kv => kv.Key.ToString(), kv => kv.Value.Value.ToString());
            Error($"JavaScript error at {_programStatus.GetLineInfo()} {ps["message"]}\n{ps["stack"]}");
        }

        public void Exception(Exception e)
        {
            switch (e)
            {
                case Jint.Runtime.JavaScriptException jse:
                    Error($"JavaScript error at {_programStatus.GetLineInfo()} {jse.Message}\n{jse.JavaScriptStackTrace}");
                    break;
                case { InnerException: Jint.Runtime.JavaScriptException jse } _:
                    Error($"JavaScript error at {_programStatus.GetLineInfo()} {jse.Message}\n{jse.JavaScriptStackTrace}");
                    break;
                default:
                    Error($"Exception at {_programStatus.GetLineInfo()} {e}");
                    break;
            }
        }

        string GuessKind(string message)
        {
            return message.StartsWith("[PlayerScript]") ? "PlayerScript" : "ScriptableItem";
        }

        int[] ParseProgramPosition()
        {
            var lineInfo = _programStatus.GetLineInfo().Split(":");
            return lineInfo.Length < 2 ? null : new [] { int.Parse(lineInfo[0]), int.Parse(lineInfo[1]) + 1 };
        }

        OutputStackItemExt[] ParseProgramStack()
        {
            try
            {
                return _programStatus.GetStack().Split("\n")
                    .Select(line =>
                    {
                        var stackInfo = line.Split(":");
                        var lineNumber = int.Parse(stackInfo[^2]);
                        var columnNumber = int.Parse(stackInfo[^1]);
                        var info = string.Join(":", stackInfo[..^2])[6..];
                        return new OutputStackItemExt
                        {
                            pos = new[]
                            {
                                lineNumber,
                                columnNumber
                            },
                            info = info
                        };
                    }).ToArray();
            }
            catch (Exception)
            {
                // _programStatus.GetStack() がうまく動かない場合もある
                return Array.Empty<OutputStackItemExt>();
            }
        }
    }
}
