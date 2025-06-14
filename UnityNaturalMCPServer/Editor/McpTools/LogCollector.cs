using System.Collections.Generic;
using UnityEngine;

namespace UnityFluxMCP.Editor.McpTools
{
    internal static class LogCollector
    {
        private static readonly Dictionary<LogType, List<LogEntry>> _logEntries = new();
        private static readonly List<LogEntry> _logHistory = new();

        public static IReadOnlyList<LogEntry> LogHistory => _logHistory;

        public static IReadOnlyList<LogEntry> GetLogHistory(LogType logTypeEnum) =>
            _logEntries.TryGetValue(logTypeEnum, out var list) ? list : new List<LogEntry>();

        public static void Initialize()
        {
            Application.logMessageReceived += LogMessageReceived;
        }

        private static void LogMessageReceived(string condition, string stackTrace, LogType type)
        {
            if (!_logEntries.TryGetValue(type, out var list))
            {
                list = new List<LogEntry>();
                _logEntries.Add(type, list);
            }

            var logEntry = new LogEntry(condition, stackTrace, type.ToString());
            list.Add(logEntry);
            _logHistory.Add(logEntry);
        }
    }

    internal record LogEntry(string Condition, string StackTrace, string Type);
}