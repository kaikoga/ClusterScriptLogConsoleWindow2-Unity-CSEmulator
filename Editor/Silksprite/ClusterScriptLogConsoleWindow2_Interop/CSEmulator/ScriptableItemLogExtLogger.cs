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

        void Log(string type, string message, int[] pos = null, OutputStackItemExt[] stack = null)
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
                kind = GuessKind(itemName, message),
                pos = pos ?? ParseProgramPosition(),
                stack = stack ?? ParseProgramStack()
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

        void Error(string message, int[] pos, OutputStackItemExt[] stack)
        {
            Log("PreviewLog_Error", message, pos, stack);
        }

        public void Exception(JsError e)
        {
            var ps = e.GetOwnProperties()
                .ToDictionary(kv => kv.Key.ToString(), kv => kv.Value.Value.ToString());
            var lineInfo = _programStatus.GetLineInfo();
            Error($"JavaScript error at {lineInfo} {ps["message"]}",
                ParseProgramPosition(lineInfo),
                ParseProgramStack(ps["stack"]));
        }

        public void Exception(Exception e)
        {
            var lineInfo = _programStatus.GetLineInfo();
            switch (e)
            {
                case Jint.Runtime.JavaScriptException jse:{}
                    Error($"JavaScript error at {lineInfo} {jse.Message}",
                        ParseProgramPosition(lineInfo),
                        ParseProgramStack(jse.JavaScriptStackTrace));
                    break;
                case { InnerException: Jint.Runtime.JavaScriptException jse } _:
                    Error($"JavaScript error at {lineInfo} {jse.Message}",
                        ParseProgramPosition(lineInfo),
                        ParseProgramStack(jse.JavaScriptStackTrace));
                    break;
                default:
                    Error($"Exception at {lineInfo} {e}");
                    break;
            }
        }

        static string GuessKind(string itemName, string message) =>
            (itemName, message) switch
            {
                // XXX This does not work
                ("PlayerScript", _) => "PlayerScript",
                var (_, m) when m.StartsWith("PlayerScript") => "PlayerScript",
                _ => "ItemScript"
            };

        int[] ParseProgramPosition(string lineInfo = null)
        {
            var parsedInfo = (lineInfo ?? _programStatus.GetLineInfo()).Split(":");
            return parsedInfo.Length < 2 ? null : new [] { int.Parse(parsedInfo[0]), int.Parse(parsedInfo[1]) + 1 };
        }

        OutputStackItemExt[] ParseProgramStack(string stack = null)
        {
            try
            {
                stack ??= _programStatus.GetStack();
                return stack.Split("\n")
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
