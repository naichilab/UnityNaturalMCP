using System.Net;
using System.Text.Json;
using UnityNaturalMCP.Editor.Managers;

namespace UnityNaturalMCP.Editor.RequestHandlers
{
    public class CompilationRequestHandler : BaseRequestHandler
    {
        private readonly CompilationManager _compilationManager;
        
        public CompilationRequestHandler()
        {
            _compilationManager = new CompilationManager();
        }
        
        public override string HandleRequest(HttpListenerRequest request)
        {
            switch (request.Url.AbsolutePath)
            {
                case "/api/project/compile":
                    return HandleCompileProject(request);
                default:
                    return CreateErrorResponse("Compilation endpoint not found");
            }
        }
        
        private string HandleCompileProject(HttpListenerRequest request)
        {
            // CompilationManager内部でタイムアウト処理を行うため、
            // ここでは追加のタイムアウトを設定しない
            try
            {
                // UniTaskを同期的に実行
                var compilationTask = _compilationManager.CompileProjectAsync();
                var result = compilationTask.GetAwaiter().GetResult();
                
                return JsonSerializer.Serialize(result);
            }
            catch (global::System.Exception ex)
            {
                return CreateErrorResponse($"Compilation failed: {ex.Message}");
            }
        }
    }
}