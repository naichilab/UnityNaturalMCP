using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityFluxMCP.Editor.Managers
{
    public class ConsoleLogManager
    {
        // 定数
        private const int MaxLogEntries = 1000;
        private const int MaxLogLimit = 200;
        private const int MinLogLimit = 1;
        private const int DefaultLogLimit = 50;
        
        private static List<LogEntry> _collectedLogs = new List<LogEntry>();
        private static readonly object _logLock = new object();
        private static bool _isListening = false;
        
        static ConsoleLogManager()
        {
            StartLogCollection();
        }
        
        private static void StartLogCollection()
        {
            if (!_isListening)
            {
                Application.logMessageReceivedThreaded += OnLogMessageReceived;
                _isListening = true;
            }
        }
        
        private static void OnLogMessageReceived(string logString, string stackTrace, LogType type)
        {
            lock (_logLock)
            {
                var logEntry = new LogEntry
                {
                    message = logString,
                    stackTrace = stackTrace,
                    type = type,
                    timestamp = DateTime.Now
                };
                
                _collectedLogs.Add(logEntry);
                
                // 最大件数を超えたら古いログを削除
                if (_collectedLogs.Count > MaxLogEntries)
                {
                    _collectedLogs.RemoveAt(0);
                }
            }
        }
        
        public object GetConsoleLogs(int limit, string logType, bool includeStackTrace)
        {
            try
            {
                limit = ClampLimit(limit);
                var logs = GetFilteredLogs(limit, logType, includeStackTrace);
                return CreateSuccessResponse(logs, limit, logType, includeStackTrace);
            }
            catch (Exception ex)
            {
                return CreateErrorResponse(ex.Message);
            }
        }
        
        /// <summary>
        /// limitを適切な範囲に制限します
        /// </summary>
        private static int ClampLimit(int limit)
        {
            return global::System.Math.Max(MinLogLimit, global::System.Math.Min(limit, MaxLogLimit));
        }
        
        /// <summary>
        /// フィルタリングされたログを取得します
        /// </summary>
        private List<object> GetFilteredLogs(int limit, string logType, bool includeStackTrace)
        {
            var logsCopy = GetLogsCopy();
            var filteredLogs = FilterLogsByType(logsCopy, logType);
            var recentLogs = filteredLogs.TakeLast(limit).ToList();
            
            return recentLogs.Select((log, index) => CreateLogEntry(log, index, includeStackTrace)).ToList();
        }
        
        /// <summary>
        /// ログのコピーを取得します
        /// </summary>
        private List<LogEntry> GetLogsCopy()
        {
            lock (_logLock)
            {
                return new List<LogEntry>(_collectedLogs);
            }
        }
        
        /// <summary>
        /// ログタイプによってフィルタリングします
        /// </summary>
        private static List<LogEntry> FilterLogsByType(List<LogEntry> logs, string logType)
        {
            return logs.Where(log => ShouldIncludeLog(log.type, logType)).ToList();
        }
        
        /// <summary>
        /// ログエントリオブジェクトを作成します
        /// </summary>
        private static object CreateLogEntry(LogEntry log, int index, bool includeStackTrace)
        {
            return new
            {
                log.message,
                file = "",
                line = 0,
                column = 0,
                type = GetLogTypeString(log.type),
                index,
                stackTrace = includeStackTrace ? log.stackTrace : "",
                timestamp = log.timestamp.ToString("HH:mm:ss.fff")
            };
        }
        
        /// <summary>
        /// 成功レスポンスを作成します
        /// </summary>
        private static object CreateSuccessResponse(List<object> logs, int limit, string logType, bool includeStackTrace)
        {
            return new
            {
                success = true,
                logs,
                count = logs.Count,
                limit,
                filter = logType ?? "all",
                includeStackTrace
            };
        }
        
        /// <summary>
        /// エラーレスポンスを作成します
        /// </summary>
        private static object CreateErrorResponse(string errorMessage)
        {
            return new { success = false, error = errorMessage };
        }
        
        private static bool ShouldIncludeLog(LogType logType, string requestedLogType)
        {
            if (string.IsNullOrEmpty(requestedLogType) || requestedLogType == "all")
                return true;
                
            return requestedLogType.ToLower() switch
            {
                "error" => logType == LogType.Error || logType == LogType.Exception || logType == LogType.Assert,
                "warning" => logType == LogType.Warning,
                "info" => logType == LogType.Log,
                _ => true
            };
        }
        
        private static string GetLogTypeString(LogType logType)
        {
            return logType switch
            {
                LogType.Error => "Error",
                LogType.Assert => "Assert",
                LogType.Warning => "Warning",
                LogType.Log => "Log",
                LogType.Exception => "Exception",
                _ => "Unknown"
            };
        }
        
        private class LogEntry
        {
            public string message { get; set; }
            public string stackTrace { get; set; }
            public LogType type { get; set; }
            public DateTime timestamp { get; set; }
        }
    }
}