using System;
using System.Collections.Generic;
using System.ComponentModel;
using Cysharp.Threading.Tasks;
using ModelContextProtocol.Server;
using UnityEditor;
using UnityEngine;

namespace UnityNaturalMCP.Editor.McpTools
{
    [McpServerToolType, Description("Control Unity Editor tools")]
    internal sealed class McpUnityEditorTool
    {
        [McpServerTool, Description("Execute AssetDatabase.Refresh")]
        public async UniTask RefreshAssets()
        {
            await UniTask.SwitchToMainThread();
            AssetDatabase.Refresh();
        }
        [McpServerTool, Description("Get log history.")]
        public IReadOnlyList<LogEntry> GetLogHistory(
            [Description("Filter logs by type. Valid values: \"All\", \"Error\", \"Assert\", \"Warning\", \"Exception\"")]
            string logType)
        {
            if (logType.ToLower() == "all")
            {
                return LogCollector.LogHistory;
            }

            var logTypeEnum = logType.ToLower() switch
            {
                "error" => LogType.Error,
                "assert" => LogType.Assert,
                "warning" => LogType.Warning,
                "log" => LogType.Log,
                "exception" => LogType.Exception,
                _ => throw new ArgumentOutOfRangeException(nameof(logType), logType, null)
            };

            return LogCollector.GetLogHistory(logTypeEnum);
        }
    }
}