using System.Net;

namespace UnityFluxMCP.Editor.RequestHandlers
{
    public interface IRequestHandler
    {
        string HandleRequest(HttpListenerRequest request);
    }
}