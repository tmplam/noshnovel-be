#nullable disable

namespace NoshNovel.Models
{
    public class NovelDetail
    {
        public string Title { get; set; }
        public Author Author { get; set; }
        public string Description { get; set; }
        public double Rating { get; set; }
        public int ReviewsNumber { get; set; }
        public string Status { get; set; }
        public string CoverImage { get; set; }
        public IEnumerable<Genre> Genres { get; set; }
    }
}
