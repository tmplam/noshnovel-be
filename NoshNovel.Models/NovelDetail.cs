#nullable disable

namespace NoshNovel.Models
{
    public class NovelDetail
    {
        public string Title { get; set; }
        public string Author { get; set; }
        public string Description { get; set; }
        public double Rating { get; set; }
        public string Status { get; set; }
        public string CoverImage { get; set; }
        public IEnumerable<Genre> Genres { get; set; }
        public IEnumerable<Chapter> Chapters { get; set; }
    }
}
