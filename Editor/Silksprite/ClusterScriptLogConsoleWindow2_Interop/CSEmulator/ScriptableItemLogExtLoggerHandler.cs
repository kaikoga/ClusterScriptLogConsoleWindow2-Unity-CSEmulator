using System;
using System.Linq;
using Assets.KaomoLab.CSEmulator;
using Assets.KaomoLab.CSEmulator.Components;
using Assets.KaomoLab.CSEmulator.Editor.Preview;
using ClusterVR.CreatorKit.Item;
using Jint.Native;
using Silksprite.ClusterScriptLogConsoleWindow2.Format;
using UnityEditor;

namespace Silksprite.ClusterScriptLogConsoleWindow2.Interop.CSEmulator
{
    public static class ScriptableItemLogExtLoggerHandler
    {
        static IPlayerMeta PlayerMeta => Bootstrap.optionBridge;

        [InitializeOnLoadMethod]
        static void Initialize()
        {
            Bootstrap.OnScriptLogged += OnScriptLogged;
        }

        static void OnScriptLogged(LogSource source, LogLevel logLevel, ScriptLoggedArgs args)
        {
            var item = args.gameObject.GetComponent<IItem>();

            switch (logLevel)
            {
                case LogLevel.Info:
                    Log("PreviewLog_Information", item, source, args.programStatus, args.message);
                    break;
                case LogLevel.Warning:
                    Log("PreviewLog_Warning", item, source, args.programStatus, args.message);
                    break;
                case LogLevel.Error:
                    Log("PreviewLog_Error", item, source, args.programStatus, args.message);
                    break;
                case LogLevel.Exception:
                    Exception(item, source, args.programStatus, args.exception);
                    break;
                case LogLevel.JsError:
                    JsError(item, source, args.programStatus, args.jsError);
                    break;
            }
        }

        static void Log(string type, IItem item, LogSource source, IProgramStatus programStatus, string message)
        {
            DoLog(type, item, source, message, ParseProgramPosition(programStatus), ParseProgramStack(programStatus));
        }

        static void JsError(IItem item, LogSource source, IProgramStatus programStatus, JsError jsError)
        {
            var ps = jsError.GetOwnProperties()
                .ToDictionary(kv => kv.Key.ToString(), kv => kv.Value.Value.ToString());
            var lineInfo = programStatus.GetLineInfo();
            DoLog("PreviewLog_Error", item, source,
                $"JavaScript error at {lineInfo} {ps["message"]}",
                ParseProgramPosition(programStatus, lineInfo),
                ParseProgramStack(programStatus, ps["stack"]));
        }

        static void Exception(IItem item, LogSource source, IProgramStatus programStatus, Exception exception)
        {
            var lineInfo = programStatus.GetLineInfo();
            switch (exception)
            {
                case Jint.Runtime.JavaScriptException jse:{}
                    DoLog("PreviewLog_Error", item, source,
                        $"JavaScript error at {lineInfo} {jse.Message}",
                        ParseProgramPosition(programStatus, lineInfo),
                        ParseProgramStack(programStatus, jse.JavaScriptStackTrace));
                    break;
                case { InnerException: Jint.Runtime.JavaScriptException jse } _:
                    DoLog("PreviewLog_Error", item, source, 
                        $"JavaScript error at {lineInfo} {jse.Message}",
                        ParseProgramPosition(programStatus, lineInfo),
                        ParseProgramStack(programStatus, jse.JavaScriptStackTrace));
                    break;
                default:
                    DoLog("PreviewLog_Error", item, source,
                        $"Exception at {lineInfo} {exception}", 
                        ParseProgramPosition(programStatus),
                        ParseProgramStack(programStatus));
                    break;
            }
        }

        static void DoLog(string type, IItem item, LogSource source, string message, int[] pos, OutputStackItemExt[] stack)
        {
            var (itemId, itemName) = item != null ? (item.Id.Value, item.ItemName) : (0L, "");
            var (userId, userName) = PlayerMeta != null ? (PlayerMeta.userId, PlayerMeta.userDisplayName) : ("", "");

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
                kind = GuessKind(source),
                pos = pos,
                stack = stack
            });
        }

        static string GuessKind(LogSource source) =>
            source switch
            {
                LogSource.ItemScript => "ItemScript",
                LogSource.PlayerScript => "PlayerScript",
                _ => "ItemScript"
            };

        static int[] ParseProgramPosition(IProgramStatus programStatus, string lineInfo = null)
        {
            var parsedInfo = (lineInfo ?? programStatus.GetLineInfo()).Split(":");
            return parsedInfo.Length < 2 ? null : new [] { int.Parse(parsedInfo[0]), int.Parse(parsedInfo[1]) + 1 };
        }

        static OutputStackItemExt[] ParseProgramStack(IProgramStatus programStatus, string stack = null)
        {
            try
            {
                stack ??= programStatus.GetStack();
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
