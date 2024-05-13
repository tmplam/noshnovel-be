namespace NoshNovel.Server.TruyenFullStrategy
{
    public partial class TruyenFullCrawlerStrategy
    {
        private static readonly string baseUrl = "https://truyenfull.vn";
        // Number of maximum novels per search page of crawled page
        private static readonly int maxPerCrawlPage = 27;
        // Number of maximum chapters per detail page of crawled detail page
        private static readonly int maxPerCrawledChaptersPage = 50;
    }
}
