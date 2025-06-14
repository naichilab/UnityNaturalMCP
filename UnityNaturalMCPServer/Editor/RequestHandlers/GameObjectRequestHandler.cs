using System;
using System.Net;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace UnityFluxMCP.Editor.RequestHandlers
{
    public class GameObjectRequestHandler : BaseRequestHandler
    {
        public override string HandleRequest(HttpListenerRequest request)
        {
            switch (request.Url.AbsolutePath)
            {
                case "/api/gameobject/create":
                    return HandleGameObjectCreate(request);
                default:
                    return CreateErrorResponse("GameObject endpoint not found");
            }
        }
        
        private string HandleGameObjectCreate(HttpListenerRequest request)
        {
            var data = ParseJsonBody(request);
            var name = data.ContainsKey("name") ? data["name"].GetString() : "GameObject";
            var primitiveType = data.ContainsKey("primitiveType") ? data["primitiveType"].GetString() : null;

            var task = UniTask.Create(async () =>
            {
                await UniTask.SwitchToMainThread();
                
                try
                {
                    GameObject newObject;
                    
                    if (!string.IsNullOrEmpty(primitiveType))
                    {
                        if (Enum.TryParse<PrimitiveType>(primitiveType, true, out var primitive))
                        {
                            newObject = GameObject.CreatePrimitive(primitive);
                            newObject.name = name;
                        }
                        else
                        {
                            return (object)new { error = $"Invalid primitive type: {primitiveType}" };
                        }
                    }
                    else
                    {
                        newObject = new GameObject(name);
                    }
                    
                    return (object)new
                    {
                        success = true,
                        message = $"GameObject '{name}' created successfully",
                        gameObjectName = newObject.name
                    };
                }
                catch (Exception e)
                {
                    return (object)new { error = e.Message };
                }
            });
            
            var result = task.GetAwaiter().GetResult();

            return CreateSuccessResponse(result);
        }
    }
}