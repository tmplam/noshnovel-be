namespace NoshNovel.Servers.TangThuVien
{
    public partial class TangThuVienCrawler
    {
        private static readonly string baseUrl = "https://truyen.tangthuvien.vn";
        // Number of maximum novels per search page of crawled page
        private static readonly int maxPerCrawlPage = 20;
        // Number of maximum chapters per detail page of crawled detail page
        private static readonly int maxPerCrawledChaptersPage = 75;
    }
}
