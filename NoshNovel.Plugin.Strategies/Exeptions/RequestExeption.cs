using System.Net;

namespace NoshNovel.Plugin.Strategies.Exeptions
{
    public class RequestExeption : Exception
    {
        public HttpStatusCode StatusCode { get; set; }

        public RequestExeption(HttpStatusCode statusCode, string message) : base(message)
        {
            StatusCode = statusCode;
        }
    }
}
