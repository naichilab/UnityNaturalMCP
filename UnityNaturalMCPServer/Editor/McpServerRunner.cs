using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityNaturalMCP.Editor.McpTools;

namespace UnityNaturalMCP.Editor
{
    internal static class McpServerRunner
    {
        private static CancellationTokenSource _cancellationTokenSource;
        private static McpServerApplication _mcpServerApplication;

        [InitializeOnLoadMethod]
        private static void Init()
        {
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.AddTo(Application.exitCancellationToken);
            _mcpServerApplication = new McpServerApplication();
            _mcpServerApplication.Run(cancellationTokenSource.Token).Forget();

            AssemblyReloadEvents.beforeAssemblyReload += () =>
            {
                _cancellationTokenSource?.Cancel();
                _mcpServerApplication?.Dispose();
                cancellationTokenSource = null;
                _mcpServerApplication = null;
            };
        }

        public static void RefreshMcpServer()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = new CancellationTokenSource();

            _mcpServerApplication?.Dispose();
            _mcpServerApplication = new McpServerApplication();
            _mcpServerApplication.Run(_cancellationTokenSource.Token).Forget();
        }
    }
}