using System.Linq;
using System.Net;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnityFluxMCP.Editor.RequestHandlers
{
    public class SceneRequestHandler : BaseRequestHandler
    {
        public override string HandleRequest(HttpListenerRequest request)
        {
            switch (request.Url.AbsolutePath)
            {
                case "/api/scene/info":
                    return HandleSceneInfo(request);
                case "/api/scene/current":
                    return HandleCurrentScene(request);
                case "/api/scene/gameobjects":
                    return HandleSceneGameObjects(request);
                default:
                    return CreateErrorResponse("Scene endpoint not found");
            }
        }
        
        private string HandleSceneInfo(HttpListenerRequest request)
        {
            var data = ParseJsonBody(request);
            var scenePath = data.ContainsKey("scenePath") ? data["scenePath"].GetString() : null;

            if (string.IsNullOrEmpty(scenePath))
            {
                return CreateErrorResponse("scenePath is required");
            }

            var task = UniTask.Create(async () =>
            {
                await UniTask.SwitchToMainThread();
                
                var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
                if (sceneAsset == null)
                {
                    return (object)new { error = $"Scene not found: {scenePath}" };
                }

                var guid = AssetDatabase.AssetPathToGUID(scenePath);
                var dependencies = AssetDatabase.GetDependencies(scenePath, false);
                
                return (object)new
                {
                    sceneAsset.name,
                    path = scenePath,
                    guid,
                    dependencies,
                    dependencyCount = dependencies.Length
                };
            });
            
            var result = task.GetAwaiter().GetResult();

            return CreateSuccessResponse(result);
        }
        
        private string HandleCurrentScene(HttpListenerRequest request)
        {
            var task = UniTask.Create(async () =>
            {
                await UniTask.SwitchToMainThread();
                
                var currentScene = SceneManager.GetActiveScene();
                
                return (object)new
                {
                    currentScene.name,
                    currentScene.path,
                    currentScene.buildIndex,
                    currentScene.isLoaded,
                    currentScene.isDirty,
                    currentScene.rootCount
                };
            });
            
            var result = task.GetAwaiter().GetResult();

            return CreateSuccessResponse(result);
        }
        
        private string HandleSceneGameObjects(HttpListenerRequest request)
        {
            var task = UniTask.Create(async () =>
            {
                await UniTask.SwitchToMainThread();
                
                var currentScene = SceneManager.GetActiveScene();
                var rootGameObjects = currentScene.GetRootGameObjects();
                
                var gameObjects = rootGameObjects.Select(go => new
                {
                    go.name,
                    go.tag,
                    layer = LayerMask.LayerToName(go.layer),
                    isActive = go.activeSelf,
                    go.transform.childCount,
                    components = go.GetComponents<Component>().Select(c => c.GetType().Name).ToList()
                }).ToList();
                
                return (object)new
                {
                    sceneName = currentScene.name,
                    gameObjects,
                    count = gameObjects.Count
                };
            });
            
            var result = task.GetAwaiter().GetResult();

            return CreateSuccessResponse(result);
        }
    }
}