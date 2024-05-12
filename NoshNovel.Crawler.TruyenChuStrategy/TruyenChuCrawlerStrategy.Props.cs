namespace NoshNovel.Server.TruyenChuStrategy
{
    public partial class TruyenChuCrawlerStrategy
    {
        private static readonly string baseUrl = "https://truyenchu.com.vn";
        // Number of maximum novels per search page of crawled page
        private static readonly int maxPerCrawlPage = 20;
        // Number of maximum chapters per detail page of crawled detail page
        private static readonly int maxPerCrawledChaptersPage = 50;

        private class CrawlChapter
        {
            public string name { get; set; }
            public string slug { get; set; }
        }
    }
}
