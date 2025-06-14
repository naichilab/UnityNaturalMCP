using System.IO;
using System.Linq;
using System.Net;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace UnityNaturalMCP.Editor.RequestHandlers
{
    public class AssetRequestHandler : BaseRequestHandler
    {
        public override string HandleRequest(HttpListenerRequest request)
        {
            switch (request.Url.AbsolutePath)
            {
                case "/api/assets/search":
                    return HandleAssetSearch(request);
                case "/api/assets/meta":
                    return HandleAssetMeta(request);
                case "/api/prefab/info":
                    return HandlePrefabInfo(request);
                case "/api/script/create":
                    return HandleScriptCreate(request);
                case "/api/prefab/create":
                    return HandlePrefabCreate(request);
                case "/api/assets/refresh":
                    return HandleAssetRefresh(request);
                default:
                    return CreateErrorResponse("Asset endpoint not found");
            }
        }
        
        private string HandleAssetSearch(HttpListenerRequest request)
        {
            var data = ParseJsonBody(request);
            var searchPattern = data.ContainsKey("searchPattern") ? data["searchPattern"].GetString() : null;
            var searchPath = data.ContainsKey("searchPath") ? data["searchPath"].GetString() : "Assets";

            if (string.IsNullOrEmpty(searchPattern))
            {
                return CreateErrorResponse("searchPattern is required");
            }

            var result = global::System.Threading.Tasks.Task.Run(async () =>
            {
                await UniTask.SwitchToMainThread();
                var filter = ConvertSearchPatternToFilter(searchPattern);
                var assets = AssetDatabase.FindAssets(filter, new[] { searchPath });
                var assetPaths = assets.Select(guid => AssetDatabase.GUIDToAssetPath(guid))
                    .Where(path => path.StartsWith(searchPath))
                    .ToList();
                
                assetPaths = FilterByExtension(assetPaths, searchPattern);
                
                return (object)new
                {
                    results = assetPaths,
                    count = assetPaths.Count
                };
            }).Result;

            return CreateSuccessResponse(result);
        }
        
        private string ConvertSearchPatternToFilter(string searchPattern)
        {
            if (searchPattern.StartsWith("*."))
            {
                var extension = searchPattern.Substring(2);
                return extension switch
                {
                    "cs" => "t:Script",
                    "prefab" => "t:Prefab",
                    "mat" => "t:Material",
                    "shader" => "t:Shader",
                    "png" or "jpg" or "jpeg" => "t:Texture2D",
                    "unity" => "t:Scene",
                    _ => searchPattern.Replace("*.", "")
                };
            }
            return searchPattern;
        }
        
        private global::System.Collections.Generic.List<string> FilterByExtension(global::System.Collections.Generic.List<string> assetPaths, string searchPattern)
        {
            if (searchPattern.StartsWith("*.") && 
                !IsKnownUnityType(searchPattern))
            {
                var extension = searchPattern.Substring(1);
                return assetPaths.Where(path => path.EndsWith(extension)).ToList();
            }
            return assetPaths;
        }
        
        private bool IsKnownUnityType(string pattern)
        {
            var knownTypes = new[] { "*.cs", "*.prefab", "*.mat", "*.shader", "*.unity" };
            return knownTypes.Contains(pattern);
        }
        
        private string HandleAssetMeta(HttpListenerRequest request)
        {
            var data = ParseJsonBody(request);
            var assetPath = data.ContainsKey("assetPath") ? data["assetPath"].GetString() : null;

            if (string.IsNullOrEmpty(assetPath))
            {
                return CreateErrorResponse("assetPath is required");
            }

            var result = global::System.Threading.Tasks.Task.Run(async () =>
            {
                await UniTask.SwitchToMainThread();
                var guid = AssetDatabase.AssetPathToGUID(assetPath);
                if (string.IsNullOrEmpty(guid))
                {
                    return (object)new { error = $"Asset not found: {assetPath}" };
                }

                var metaPath = assetPath + ".meta";
                var metaExists = File.Exists(metaPath);
                
                return (object)new
                {
                    path = assetPath,
                    guid,
                    metaExists,
                    type = AssetDatabase.GetMainAssetTypeAtPath(assetPath)?.Name
                };
            }).Result;

            return CreateSuccessResponse(result);
        }
        
        private string HandlePrefabInfo(HttpListenerRequest request)
        {
            var data = ParseJsonBody(request);
            var prefabPath = data.ContainsKey("prefabPath") ? data["prefabPath"].GetString() : null;

            if (string.IsNullOrEmpty(prefabPath))
            {
                return CreateErrorResponse("prefabPath is required");
            }

            var result = global::System.Threading.Tasks.Task.Run(async () =>
            {
                await UniTask.SwitchToMainThread();
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefab == null)
                {
                    return (object)new { error = $"Prefab not found: {prefabPath}" };
                }

                var components = prefab.GetComponentsInChildren<Component>()
                    .GroupBy(c => c.GetType().Name)
                    .ToDictionary(g => g.Key, g => g.Count());

                return (object)new
                {
                    prefab.name,
                    path = prefabPath,
                    guid = AssetDatabase.AssetPathToGUID(prefabPath),
                    rootGameObjectName = prefab.name,
                    componentTypes = components,
                    prefab.transform.childCount
                };
            }).Result;

            return CreateSuccessResponse(result);
        }
        
        private string HandleScriptCreate(HttpListenerRequest request)
        {
            var data = ParseJsonBody(request);
            var scriptPath = data.ContainsKey("scriptPath") ? data["scriptPath"].GetString() : null;
            var className = data.ContainsKey("className") ? data["className"].GetString() : null;
            var baseClass = data.ContainsKey("baseClass") ? data["baseClass"].GetString() : "MonoBehaviour";

            if (string.IsNullOrEmpty(scriptPath) || string.IsNullOrEmpty(className))
            {
                return CreateErrorResponse("scriptPath and className are required");
            }

            var result = global::System.Threading.Tasks.Task.Run(async () =>
            {
                await UniTask.SwitchToMainThread();
                try
                {
                    var directory = Path.GetDirectoryName(scriptPath);
                    if (!Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    var scriptContent = GenerateScriptContent(className, baseClass);
                    File.WriteAllText(scriptPath, scriptContent);
                    AssetDatabase.Refresh();
                    
                    return (object)new
                    {
                        success = true,
                        message = $"Script {className} created at {scriptPath}"
                    };
                }
                catch (global::System.Exception e)
                {
                    return (object)new { error = e.Message };
                }
            }).Result;

            return CreateSuccessResponse(result);
        }
        
        private string GenerateScriptContent(string className, string baseClass)
        {
            return $@"using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class {className} : {baseClass}
{{
    void Start()
    {{
        
    }}

    void Update()
    {{
        
    }}
}}";
        }
        
        private string HandlePrefabCreate(HttpListenerRequest request)
        {
            var data = ParseJsonBody(request);
            var prefabPath = data.ContainsKey("prefabPath") ? data["prefabPath"].GetString() : null;
            var gameObjectName = data.ContainsKey("gameObjectName") ? data["gameObjectName"].GetString() : "GameObject";

            if (string.IsNullOrEmpty(prefabPath))
            {
                return CreateErrorResponse("prefabPath is required");
            }

            var result = global::System.Threading.Tasks.Task.Run(async () =>
            {
                await UniTask.SwitchToMainThread();
                try
                {
                    var directory = Path.GetDirectoryName(prefabPath);
                    if (!Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    var go = new GameObject(gameObjectName);
                    var success = PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
                    GameObject.DestroyImmediate(go);
                    
                    AssetDatabase.Refresh();
                    
                    return (object)new
                    {
                        success = success != null,
                        message = success != null 
                            ? $"Prefab {gameObjectName} created at {prefabPath}"
                            : "Failed to create prefab"
                    };
                }
                catch (global::System.Exception e)
                {
                    return (object)new { error = e.Message };
                }
            }).Result;

            return CreateSuccessResponse(result);
        }
        
        private string HandleAssetRefresh(HttpListenerRequest request)
        {
            var data = ParseJsonBody(request);
            var assetPath = data.ContainsKey("assetPath") ? data["assetPath"].GetString() : null;
            var force = data.ContainsKey("force") && data["force"].GetBoolean();

            var result = global::System.Threading.Tasks.Task.Run(async () =>
            {
                await UniTask.SwitchToMainThread();
                try
                {
                    if (string.IsNullOrEmpty(assetPath))
                    {
                        // パスが指定されていない場合は全体をリフレッシュ
                        AssetDatabase.Refresh();
                        return (object)new
                        {
                            success = true,
                            message = "AssetDatabase refreshed completely",
                            scope = "全体"
                        };
                    }
                    else
                    {
                        // 特定のアセットファイルが存在するかチェック
                        if (!File.Exists(assetPath))
                        {
                            return (object)new { error = $"File not found: {assetPath}" };
                        }

                        // 特定のアセットをリフレッシュ（metaファイル生成含む）
                        if (force)
                        {
                            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
                        }
                        else
                        {
                            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
                        }

                        // metaファイルの存在確認
                        var metaPath = assetPath + ".meta";
                        var metaExists = File.Exists(metaPath);

                        return (object)new
                        {
                            success = true,
                            message = $"Asset refreshed: {assetPath}",
                            path = assetPath,
                            metaGenerated = metaExists,
                            force
                        };
                    }
                }
                catch (global::System.Exception e)
                {
                    return (object)new { error = e.Message };
                }
            }).Result;

            return CreateSuccessResponse(result);
        }
    }
}