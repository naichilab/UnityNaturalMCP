using Microsoft.Extensions.DependencyInjection;
using UnityEngine;
using UnityFluxMCP.Editor;

namespace Editor
{
    [CreateAssetMenu(fileName = "ExampleMCPToolBuilder", menuName = "UnityFluxMCP/Example MCP Tool Builder", order = 1)]
    internal sealed class ExampleMCPToolBuilder : McpBuilderScriptableObject
    {
        public override void Build(IMcpServerBuilder builder)
        {
            builder.WithTools<ExampleMCPTool>();
        }
    }
}