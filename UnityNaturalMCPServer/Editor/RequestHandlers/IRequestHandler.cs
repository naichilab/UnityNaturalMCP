using System.Net;

namespace UnityNaturalMCP.Editor.RequestHandlers
{
    public interface IRequestHandler
    {
        string HandleRequest(HttpListenerRequest request);
    }
}