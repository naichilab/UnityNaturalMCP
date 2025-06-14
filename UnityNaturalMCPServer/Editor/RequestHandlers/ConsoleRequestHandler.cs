using System.Collections.Generic;
using System.Net;
using Cysharp.Threading.Tasks;
using UnityFluxMCP.Editor.Managers;

namespace UnityFluxMCP.Editor.RequestHandlers
{
    public class ConsoleRequestHandler : BaseRequestHandler
    {
        private readonly ConsoleLogManager _consoleLogManager;
        
        public ConsoleRequestHandler()
        {
            _consoleLogManager = new ConsoleLogManager();
        }
        
        public override string HandleRequest(HttpListenerRequest request)
        {
            switch (request.Url.AbsolutePath)
            {
                case "/api/console/logs":
                    return HandleGetConsoleLogs(request);
                default:
                    return CreateErrorResponse("Console endpoint not found");
            }
        }
        
        private string HandleGetConsoleLogs(HttpListenerRequest request)
        {
            var result = global::System.Threading.Tasks.Task.Run(async () =>
            {
                await UniTask.SwitchToMainThread();
                
                var parameters = ParseQueryString(request.Url.Query);
                var (limit, logType, includeStackTrace) = ExtractConsoleLogParameters(parameters);
                
                return _consoleLogManager.GetConsoleLogs(limit, logType, includeStackTrace);
            }).Result;

            return CreateSuccessResponse(result);
        }
        
        /// <summary>
        /// コンソールログパラメータを抽出します
        /// </summary>
        private static (int limit, string logType, bool includeStackTrace) ExtractConsoleLogParameters(Dictionary<string, string> parameters)
        {
            const int defaultLimit = 50;
            const string defaultLogType = "all";
            
            var limit = GetIntParameter(parameters, "limit", defaultLimit);
            var logType = GetStringParameter(parameters, "type", defaultLogType).ToLower();
            var includeStackTrace = GetBoolParameter(parameters, "stackTrace", false);
            
            return (limit, logType, includeStackTrace);
        }
        
        /// <summary>
        /// 整数パラメータを取得します
        /// </summary>
        private static int GetIntParameter(Dictionary<string, string> parameters, string key, int defaultValue)
        {
            return parameters.ContainsKey(key) && int.TryParse(parameters[key], out int value) 
                ? value 
                : defaultValue;
        }
        
        /// <summary>
        /// 文字列パラメータを取得します
        /// </summary>
        private static string GetStringParameter(Dictionary<string, string> parameters, string key, string defaultValue)
        {
            return parameters.ContainsKey(key) ? parameters[key] : defaultValue;
        }
        
        /// <summary>
        /// ブール値パラメータを取得します
        /// </summary>
        private static bool GetBoolParameter(Dictionary<string, string> parameters, string key, bool defaultValue)
        {
            return parameters.ContainsKey(key) && bool.TryParse(parameters[key], out bool value) 
                ? value 
                : defaultValue;
        }
    }
}