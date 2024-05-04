namespace NoshNovel.Servers.TruyenChu
{
    public partial class TruyenChuCrawler
    {
        private static readonly string baseUrl = "https://truyenchu.com.vn";
        // Number of maximum novels per search page of crawled page
        private static readonly int maxPerCrawlPage = 20;
        // Number of maximum chapters per detail page of crawled detail page
        private static readonly int maxPerCrawledChaptersPage = 20;
    }
}
