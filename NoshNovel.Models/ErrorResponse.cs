#nullable disable

namespace NoshNovel.Models
{
    public class ErrorResponse
    {
        public Guid ErrorId { get; set; }
        public string RequestUrl { get; set; }
        public string ErrorMessage { get; set; }
    }
}
