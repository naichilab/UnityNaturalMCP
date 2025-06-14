using System.ComponentModel;
using ModelContextProtocol.Server;
using UnityEngine;

namespace Editor
{
    [McpServerToolType, Description("Example MCP tool for Unity Natural MCP.")]
    internal sealed class ExampleMCPTool : MonoBehaviour
    {
        [McpServerTool, Description("Retrurns \"Hello World!\" message.")]
        public string Hello()
        {
            return "Hello World!";
        }
    }
}
