namespace NoshNovel.Server.SanTruyenStrategy
{
    public partial class SanTruyenCrawlerStrategy
    {
        private static readonly string baseUrl = "https://santruyen.com";
        // Number of maximum novels per search page of crawled page
        private static readonly int maxPerCrawlPage = 25;
        // Number of maximum chapters per detail page of crawled detail page
        private static readonly int maxPerCrawledChaptersPage = 50;
    }
}
