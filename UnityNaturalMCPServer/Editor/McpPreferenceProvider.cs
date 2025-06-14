using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UnityNaturalMCP.Editor
{
    internal class McpPreferenceProvider : SettingsProvider
    {
        private const string SettingPath = "Preferences/Unity Natural MCP";
        private readonly UnityEditor.Editor _preferenceEditor;

        [SettingsProvider]
        public static SettingsProvider CreateSettingProvider()
        {
            return new McpPreferenceProvider(SettingPath, SettingsScope.User, null);
        }

        public McpPreferenceProvider(string path, SettingsScope scopes, IEnumerable<string> keywords) : base(path, scopes, keywords)
        {
            var preferences = McpPreference.instance;
            preferences.hideFlags = HideFlags.HideAndDontSave & ~HideFlags.NotEditable; 
            UnityEditor.Editor.CreateCachedEditor(preferences, null, ref _preferenceEditor);
        }

        public override void OnGUI(string searchContext)
        {
            EditorGUI.BeginChangeCheck();
            _preferenceEditor.OnInspectorGUI();
            if (GUILayout.Button("Refresh"))
            {
                McpServerRunner.RefreshMcpServer();
            }
            if (EditorGUI.EndChangeCheck())
            {
                McpPreference.instance.Save();
            }
        }
    }
}