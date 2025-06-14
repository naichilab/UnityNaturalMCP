using UnityEditor;

namespace UnityFluxMCP.Editor
{
    [FilePath("UnityFluxMcpPreferences.asset", FilePathAttribute.Location.PreferencesFolder)]
    public class McpPreference : ScriptableSingleton<McpPreference>
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