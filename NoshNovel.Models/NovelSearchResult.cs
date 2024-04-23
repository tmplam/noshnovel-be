#nullable disable

namespace NoshNovel.Models
{
    public class NovelSearchResult
    {
        public int Page { get; set; }
        public int PerPage { get; set; }
        public int Total { get; set; }
        public int TotalPages { get; set; }
        public IEnumerable<NovelItem> Data { get; set; }
    }
}
