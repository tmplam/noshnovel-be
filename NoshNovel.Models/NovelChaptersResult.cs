namespace NoshNovel.Models
{
    public class NovelChaptersResult
    {
        public int Page { get; set; }
        public int PerPage { get; set; }
        public int Total { get; set; }
        public int TotalPages { get; set; }
        public IEnumerable<Chapter> Data { get; set; }
    }
}
