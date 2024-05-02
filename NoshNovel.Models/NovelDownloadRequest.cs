namespace NoshNovel.Models
{
    public class NovelDownloadRequest
    {
        public string Server { get; set; }
        public string FileExtension { get; set; }
        public string NovelSlug { get; set; }
        public string NovelStyling { get; set; }
        public IEnumerable<string> ChapterSlugs { get; set; }
    }
}
