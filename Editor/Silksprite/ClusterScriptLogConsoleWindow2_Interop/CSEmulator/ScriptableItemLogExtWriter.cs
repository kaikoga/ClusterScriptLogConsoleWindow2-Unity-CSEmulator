using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Silksprite.ClusterScriptLogConsoleWindow2.Format;
using UnityEditor;
using UnityEngine;

namespace Silksprite.ClusterScriptLogConsoleWindow2.Interop.CSEmulator
{
    public static class ScriptableItemLogExtWriter
    {
        static readonly ConcurrentQueue<OutputScriptableItemLogExt> LogEntries = new();

        static CancellationTokenSource cancellationTokenSource;

        [InitializeOnLoadMethod]
        static void Initialize()
        {
            EditorApplication.playModeStateChanged += PlayModeStateChanged;
        }

        static void PlayModeStateChanged(PlayModeStateChange playMode)
        {
            switch (playMode)
            {
                case PlayModeStateChange.ExitingEditMode:
                    File.Delete(LogFileWatcherConstants.EditorPreviewLogFilePath);
                    break;
                case PlayModeStateChange.EnteredPlayMode:
                    Cancel();
                    cancellationTokenSource = new CancellationTokenSource();
                    var _ = WriteLogAsync(cancellationTokenSource.Token);
                    break;
                case PlayModeStateChange.ExitingPlayMode:
                    Cancel();
                    break;
            }
        }

        public static void Enqueue(OutputScriptableItemLogExt item)
        {
            LogEntries.Enqueue(item);
        }

        static async Task WriteLogAsync(CancellationToken cancellationToken)
        {
            try
            {
                var fileStream = File.Open(LogFileWatcherConstants.EditorPreviewLogFilePath, FileMode.Append, FileAccess.Write, FileShare.Read);
                await using var logWriter = new StreamWriter(fileStream);
                logWriter.AutoFlush = true;
                while (!cancellationToken.IsCancellationRequested)
                {
                    var sb = new StringBuilder();
                    while (LogEntries.TryDequeue(out var logEntry))
                    {
                        sb.AppendLine(JsonUtility.ToJson(logEntry, false));
                    }
                    // ReSharper disable once MethodHasAsyncOverload
                    logWriter.Write(sb.ToString());
                    await Task.Delay(TimeSpan.FromMilliseconds(200), cancellationToken);
                }
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                Debug.LogException(e);
            }
        }

        static void Cancel()
        {
            cancellationTokenSource?.Cancel();
            cancellationTokenSource?.Dispose();
            cancellationTokenSource = null;
        }
    }
}
