using UnityEditor;

namespace UnityNaturalMCP.Editor
{
    [FilePath("UnityNaturalMCPSetting.asset", FilePathAttribute.Location.ProjectFolder)]
    public sealed class MCPSetting : ScriptableSingleton<MCPSetting>
    {
        public string ipAddress = "localhost";
        public int port = 8090;
        public bool showMcpServerLog = true;

        public void Save()
        {
            Save(true);
        }
    }
}