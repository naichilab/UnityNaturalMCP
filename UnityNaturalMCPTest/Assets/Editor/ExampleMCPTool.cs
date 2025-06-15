using System.ComponentModel;
using ModelContextProtocol.Server;

namespace Editor
{
    [McpServerToolType, Description("Example MCP tool for Unity Natural MCP.")]
    internal sealed class ExampleMCPTool
    {
        [McpServerTool, Description("Retrurns \"Hello World!\" message.")]
        public string Hello()
        {
            return "Hello World!";
        }
    }
}
