#nullable disable

namespace NoshNovel.Models
{
    public class NovelItem
    {
        public string Title { get; set; }
        public Author Author { get; set; }
        public string CoverImage { get; set; }
        public string NovelSlug { get; set; }
        public string Status { get; set; }
        public IEnumerable<Genre> Genres { get; set; }
        public int TotalChapter { get; set; }
    }
}
