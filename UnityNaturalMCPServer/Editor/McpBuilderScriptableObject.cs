using Microsoft.Extensions.DependencyInjection;
using UnityEngine;

namespace UnityFluxMCP.Editor
{
    public abstract class McpBuilderScriptableObject : ScriptableObject
    {
        public abstract void Build(IMcpServerBuilder builder);
    }
}