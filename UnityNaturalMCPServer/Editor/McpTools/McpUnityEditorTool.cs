using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using ModelContextProtocol.Server;
using UnityEditor;
using UnityEngine;
using Assert = UnityEngine.Assertions.Assert;

namespace UnityNaturalMCP.Editor.McpTools
{
    [McpServerToolType, Description("Control Unity Editor tools")]
    internal sealed class McpUnityEditorTool
    {
        [McpServerTool, Description("Execute AssetDatabase.Refresh")]
        public async UniTask RefreshAssets()
        {
            try
            {
                await UniTask.SwitchToMainThread();
                AssetDatabase.Refresh();
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                throw;
            }
        }

        [McpServerTool, Description("Get current console logs. Recommend calling ClearConsoleLogs beforehand.")]
        public async Task<IReadOnlyList<LogEntry>> GetCurrentConsoleLogs(
            [Description(
                "Filter logs by type. Valid values: \"\"(Maches all logs), \"error\", \"warning\", \"log\", \"compile-error\"(This is all you need to check for compilation errors.), \"compile-warning\"")]
            string logType = "",
            [Description("Filter by regex. If empty, all logs are returned.")]
            string filter = "",
            [Description("Log count limit. Set to 0 for no limit(Not recommended).")]
            int maxCount = 20,
            [Description("Get only first line of the log message. If false, the whole message is returned.(To save tokens, recommend calling this with true.)")]
            bool onlyFirstLine = true,
            [Description(
                "If true, the logs will be sorted by time in chronological order(oldest first). If false, newest first.")]
            bool isChronological = false)
        {
            try
            {
                await UniTask.SwitchToMainThread();
                var logTypeToLower = logType.ToLower();
                var logs = new List<LogEntry>();
                var logEntries = Type.GetType("UnityEditor.LogEntries,UnityEditor.dll");
                Assert.IsNotNull(logEntries);

                var getCountMethod = logEntries.GetMethod("GetCount", BindingFlags.Public | BindingFlags.Static);
                var getEntryInternalMethod = logEntries.GetMethod("GetEntryInternal", BindingFlags.Public | BindingFlags.Static);

                Assert.IsNotNull(getCountMethod);
                Assert.IsNotNull(getEntryInternalMethod);

                var count = (int)getCountMethod.Invoke(null, null);

                for (var i = 0; i < count; i++)
                {
                    var logEntryType = Type.GetType("UnityEditor.LogEntry,UnityEditor.dll");
                    Assert.IsNotNull(logEntryType);

                    var logEntry = Activator.CreateInstance(logEntryType);

                    getEntryInternalMethod.Invoke(null, new[] { i, logEntry });

                    var message = logEntry.GetType().GetField("message").GetValue(logEntry) as string ?? "";
                    var file = logEntry.GetType().GetField("file").GetValue(logEntry) as string ?? "";
                    var mode = (int)logEntry.GetType().GetField("mode").GetValue(logEntry);
                    var logTypeValue = UnityInternalLogModeToLogType(mode);

                    if ((string.IsNullOrEmpty(logTypeToLower) || logTypeValue.Equals(logTypeToLower))
                        && (string.IsNullOrEmpty(filter) || Regex.IsMatch(message, filter)))
                    {
                        logs.Add(new LogEntry(onlyFirstLine ? message.Split('\n')[0] : message, logTypeValue));
                    }
                }

                if (!isChronological)
                {
                    logs = ((IEnumerable<LogEntry>)logs).Reverse().ToList();
                }

                if (maxCount > 0)
                {
                    logs = logs.Take(maxCount).ToList();
                }

                return logs;
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                throw;
            }
        }

        [McpServerTool, Description("Clear console logs. It is recommended to call it before GetCurrentConsoleLogs.")]
        public async Task ClearConsoleLogs()
        {
            try
            {
                await UniTask.SwitchToMainThread();
                var logEntries = Type.GetType("UnityEditor.LogEntries,UnityEditor.dll");
                Assert.IsNotNull(logEntries);

                var clearMethod = logEntries.GetMethod("Clear", BindingFlags.Public | BindingFlags.Static);

                Assert.IsNotNull(clearMethod);

                clearMethod.Invoke(null, null);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                throw;
            }
        }

        private string UnityInternalLogModeToLogType(int mode) => mode switch
        {
            _ when (mode & (int)LogMessageFlags.ScriptingError) != 0 => "error",
            _ when (mode & (int)LogMessageFlags.ScriptingWarning) != 0 => "warning",
            _ when (mode & (int)LogMessageFlags.ScriptingLog) != 0 => "log",
            _ when (mode & (int)LogMessageFlags.ScriptCompileError) != 0 => "compile-error",
            _ when (mode & (int)LogMessageFlags.ScriptCompileWarning) != 0 => "compile-warning",
            _ => "unknown"
        };
    }
}