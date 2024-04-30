namespace NoshNovel.Models
{
    public class NovelDownloadObject
    {
        public NovelDetail NovelDetail { get; set; }
        public IEnumerable<NovelContent> DownloadChapters { get; set; }
        public string NovelStyling { get; set; }
    }
}
