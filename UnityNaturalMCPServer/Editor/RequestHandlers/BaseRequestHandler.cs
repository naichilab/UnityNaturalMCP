using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.Json;

namespace UnityNaturalMCP.Editor.RequestHandlers
{
    public abstract class BaseRequestHandler : IRequestHandler
    {
        public abstract string HandleRequest(HttpListenerRequest request);
        
        protected string ReadRequestBody(HttpListenerRequest request)
        {
            using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
            {
                return reader.ReadToEnd();
            }
        }
        
        protected Dictionary<string, JsonElement> ParseJsonBody(HttpListenerRequest request)
        {
            var body = ReadRequestBody(request);
            if (string.IsNullOrEmpty(body))
                return new Dictionary<string, JsonElement>();
                
            return JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(body) ?? new Dictionary<string, JsonElement>();
        }
        
        protected string CreateErrorResponse(string errorMessage)
        {
            return JsonSerializer.Serialize(new { error = errorMessage });
        }
        
        protected string CreateSuccessResponse(object data)
        {
            return JsonSerializer.Serialize(data);
        }
        
        protected Dictionary<string, string> ParseQueryString(string query)
        {
            var result = new Dictionary<string, string>();
            
            if (string.IsNullOrEmpty(query))
                return result;
                
            if (query.StartsWith("?"))
                query = query.Substring(1);
                
            var pairs = query.Split('&');
            
            foreach (var pair in pairs)
            {
                if (string.IsNullOrEmpty(pair))
                    continue;
                    
                var keyValue = pair.Split('=');
                if (keyValue.Length == 2)
                {
                    var key = Uri.UnescapeDataString(keyValue[0]);
                    var value = Uri.UnescapeDataString(keyValue[1]);
                    result[key] = value;
                }
                else if (keyValue.Length == 1)
                {
                    var key = Uri.UnescapeDataString(keyValue[0]);
                    result[key] = "";
                }
            }
            
            return result;
        }
    }
}