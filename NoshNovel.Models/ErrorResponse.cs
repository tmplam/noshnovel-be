#nullable disable

namespace NoshNovel.Models
{
    public class ErrorResponse
    {
        public int StatusCode { get; set; }
        public string RequestId { get; set; }
        public string Message { get; set; }
    }
}
