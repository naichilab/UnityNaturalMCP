using System.Net;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityFluxMCP.Editor.Managers;

namespace UnityFluxMCP.Editor.RequestHandlers
{
    public class ProjectRequestHandler : BaseRequestHandler
    {
        private readonly ConsoleLogManager _consoleLogManager;
        
        public ProjectRequestHandler()
        {
            _consoleLogManager = new ConsoleLogManager();
        }
        
        public override string HandleRequest(HttpListenerRequest request)
        {
            switch (request.Url.AbsolutePath)
            {
                case "/api/project/info":
                    return HandleProjectInfo(request);
                case "/api/project/compile":
                    return HandleCompileProject(request);
                default:
                    return CreateErrorResponse("Project endpoint not found");
            }
        }
        
        private string HandleProjectInfo(HttpListenerRequest request)
        {
            var result = global::System.Threading.Tasks.Task.Run(async () =>
            {
                await UniTask.SwitchToMainThread();
                return new
                {
                    projectPath = Application.dataPath.Replace("/Assets", ""),
                    projectName = Application.productName,
                    Application.unityVersion,
                    platform = Application.platform.ToString(),
                    Application.isPlaying,
                    Application.targetFrameRate,
                    systemLanguage = Application.systemLanguage.ToString(),
                    Application.companyName,
                    Application.dataPath,
                    Application.persistentDataPath
                };
            }).Result;

            return CreateSuccessResponse(result);
        }
        
        private string HandleCompileProject(HttpListenerRequest request)
        {
            try
            {
                // CompilationManagerを使用してシンプルにコンパイルを実行
                var compilationManager = new CompilationManager();
                var result = compilationManager.CompileProjectAsync().GetAwaiter().GetResult();
                return CreateSuccessResponse(result);
            }
            catch (global::System.Exception ex)
            {
                Debug.LogError($"[ProjectRequestHandler] HandleCompileProject error: {ex.Message}\n{ex.StackTrace}");
                return CreateErrorResponse($"Error during compilation: {ex.Message}");
            }
        }
    }
}