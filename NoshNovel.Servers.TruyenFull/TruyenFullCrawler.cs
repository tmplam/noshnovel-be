using HtmlAgilityPack;
using NoshNovel.Models;
using NoshNovel.Plugins;
using NoshNovel.Plugins.Attributes;
using NoshNovel.Plugins.Utilities;

namespace NoshNovel.Servers.TruyenFull
{
    [NovelServer("truyenfull.vn")]
    public class TruyenFullCrawler : INovelCrawler
    {
        private static readonly int maxPerCrawlPage = 27;

        public NovelSearchResult GetByKeyword(string keyword, int page = 1, int perPage = 18)
        {
            // Calculate page and position to crawl
            int startPosition = (page - 1) * perPage + 1;
            int firstCrawledPage = startPosition / maxPerCrawlPage + 1;
            int crawlPosition = startPosition % maxPerCrawlPage - 1;

            keyword = string.Join("%20", keyword.Split(" ", StringSplitOptions.RemoveEmptyEntries));

            // Calculate total crawled pages
            HtmlWeb web = new HtmlWeb();
            string url = $"https://truyenfull.vn/tim-kiem/?tukhoa={keyword}";
            HtmlDocument doc = web.Load(url);

            HtmlNode paginationNode = doc.DocumentNode.SelectSingleNode(".//ul[contains(@class, 'pagination')]");

            int totalCrawlPages = 1;

            if (paginationNode != null)
            {
                HtmlNode lastNode = paginationNode.Descendants("li").LastOrDefault().SelectSingleNode(".//a");
                HtmlNode penultimateNode = paginationNode.Descendants("li").LastOrDefault().PreviousSibling.SelectSingleNode(".//a");

                string pageString = penultimateNode.GetAttributeValue("title", "");

                if (lastNode.InnerText.Trim() == "Cuối &raquo;")
                {
                    pageString = lastNode.GetAttributeValue("title", "");
                }
                string[] pageStringTokens = pageString.Split(" ", StringSplitOptions.RemoveEmptyEntries);
                totalCrawlPages = int.Parse(pageStringTokens[pageStringTokens.Length - 1]);
            }

            // Crawl novel and add to list
            int novelCountDown = perPage;
            List<NovelItem> novelItems = new List<NovelItem>();

            for (int i = firstCrawledPage; i <= totalCrawlPages && novelCountDown > 0; i++)
            {
                url = $"https://truyenfull.vn/tim-kiem/?tukhoa={keyword}&page={i}";
                doc = web.Load(url);

                HtmlNodeCollection novelNodes = doc.DocumentNode.SelectNodes("//div[@class='row' and @itemscope and @itemtype='https://schema.org/Book']");

                if (novelNodes != null)
                {
                    for (int j = crawlPosition; j < novelNodes.Count(); j++)
                    {
                        NovelItem novelItem = new NovelItem();

                        HtmlNode novelNode = novelNodes[j];

                        novelItem.CoverImage = novelNode.SelectSingleNode(".//div[@class='lazyimg']").GetAttributeValue("data-image", "");
                        novelItem.Author = novelNode.SelectSingleNode(".//span[@class='author']").InnerText;
                        novelItem.Url = novelNode.SelectSingleNode(".//a[@itemprop='url']").GetAttributeValue("href", "");
                        novelItem.Title = novelNode.SelectSingleNode(".//a[@itemprop='url']").GetAttributeValue("title", "");

                        try
                        {
                            string[] chapterStringTokens = novelNode.SelectSingleNode(".//span[@class='chapter-text']").ParentNode.InnerText.Split(" ", StringSplitOptions.RemoveEmptyEntries);

                            string chapterNumString = chapterStringTokens[chapterStringTokens.Length - 1];
                            if (chapterNumString.Contains("-"))
                            {
                                chapterNumString = chapterNumString.Split("-")[0];
                            }
                            novelItem.TotalChapter = int.Parse(chapterNumString);

                        }
                        catch (Exception)
                        {
                            novelItem.TotalChapter = -1;
                        }

                        novelItem.Status = novelNode.SelectSingleNode(".//span[contains(@class, 'label-full')]") != null ? "Đã hoàn thành" : "Đang ra";

                        novelItems.Add(novelItem);

                        if (--novelCountDown == 0)
                        {
                            break;
                        }
                    }
                    crawlPosition = 0;
                }
            }

            // Calculate total novels
            url = $"https://truyenfull.vn/tim-kiem/?tukhoa={keyword}&page={totalCrawlPages}";
            doc = web.Load(url);
            int totalNovels = 0;

            HtmlNodeCollection lastPageNodes = doc.DocumentNode.SelectNodes("//div[@class='row' and @itemscope and @itemtype='https://schema.org/Book']");
            if (lastPageNodes != null)
            {
                int novelOflastCrawedPage = doc.DocumentNode.SelectNodes("//div[@class='row' and @itemscope and @itemtype='https://schema.org/Book']").Count();
                totalNovels = (totalCrawlPages - 1) * maxPerCrawlPage + novelOflastCrawedPage;
            }

            // Return object
            NovelSearchResult response = new NovelSearchResult();
            response.Page = page;
            response.PerPage = perPage;
            response.Total = totalNovels;
            response.TotalPages = totalNovels / perPage + (totalNovels % perPage == 0 ? 0 : 1);
            response.Data = novelItems;

            return response;
        }

        public NovelSearchResult FilterByGenre(string genre, int page = 1, int perPage = 18)
        {
            // Calculate page and position to crawl
            int startPosition = (page - 1) * perPage + 1;
            int firstCrawledPage = startPosition / maxPerCrawlPage + 1;
            int crawlPosition = startPosition % maxPerCrawlPage - 1;

            genre = HelperClass.GenerateSlug(genre);
            // In case first page of genre search has 26 novels and 1 novel with no content
            if (firstCrawledPage > 1 || (firstCrawledPage == 1 && crawlPosition > 4))
            {
                crawlPosition++;
                if (crawlPosition == maxPerCrawlPage)
                {
                    firstCrawledPage++;
                    crawlPosition = 0;
                }
            }

            // Calculate total crawled pages
            HtmlWeb web = new HtmlWeb();
            string url = $"https://truyenfull.vn/the-loai/{genre}/";
            HtmlDocument doc = web.Load(url);

            HtmlNode paginationNode = doc.DocumentNode.SelectSingleNode(".//ul[contains(@class, 'pagination')]");

            int totalCrawlPages = 1;

            if (paginationNode != null)
            {
                HtmlNode lastNode = paginationNode.Descendants("li").LastOrDefault().SelectSingleNode(".//a");
                HtmlNode penultimateNode = paginationNode.Descendants("li").LastOrDefault().PreviousSibling.SelectSingleNode(".//a");

                string pageString = penultimateNode.GetAttributeValue("title", "");

                if (lastNode.InnerText.Trim() == "Cuối &raquo;")
                {
                    pageString = lastNode.GetAttributeValue("title", "");
                }
                string[] pageStringTokens = pageString.Split(" ", StringSplitOptions.RemoveEmptyEntries);
                totalCrawlPages = int.Parse(pageStringTokens[pageStringTokens.Length - 1]);
            }


            // Crawl novel and add to list
            int novelCountDown = perPage;
            List<NovelItem> novelItems = new List<NovelItem>();

            for (int i = firstCrawledPage; i <= totalCrawlPages && novelCountDown > 0; i++)
            {
                url = $"https://truyenfull.vn/the-loai/{genre}/trang-{i}";
                doc = web.Load(url);

                HtmlNodeCollection novelNodes = doc.DocumentNode.SelectNodes("//div[@class='row' and @itemscope and @itemtype='https://schema.org/Book']");

                if (novelNodes != null)
                {
                    for (int j = crawlPosition; j < novelNodes.Count(); j++)
                    {
                        NovelItem novelItem = new NovelItem();

                        HtmlNode novelNode = novelNodes[j];
                        // In case first page of genre search has 26 novels and 1 novel with no content
                        if (novelNode.InnerHtml == "")
                        {
                            continue;
                        }
                        novelItem.CoverImage = novelNode.SelectSingleNode(".//div[@class='lazyimg']").GetAttributeValue("data-image", "");
                        novelItem.Author = novelNode.SelectSingleNode(".//span[@class='author']").InnerText;
                        novelItem.Url = novelNode.SelectSingleNode(".//a[@itemprop='url']").GetAttributeValue("href", "");
                        novelItem.Title = novelNode.SelectSingleNode(".//a[@itemprop='url']").GetAttributeValue("title", "");

                        try
                        {
                            string[] chapterStringTokens = novelNode.SelectSingleNode(".//span[@class='chapter-text']").ParentNode.InnerText.Split(" ", StringSplitOptions.RemoveEmptyEntries);

                            string chapterNumString = chapterStringTokens[chapterStringTokens.Length - 1];
                            if (chapterNumString.Contains("-"))
                            {
                                chapterNumString = chapterNumString.Split("-")[0];
                            }
                            novelItem.TotalChapter = int.Parse(chapterNumString);

                        }
                        catch (Exception)
                        {
                            novelItem.TotalChapter = -1;
                        }

                        novelItem.Status = novelNode.SelectSingleNode(".//span[contains(@class, 'label-full')]") != null ? "Đã hoàn thành" : "Đang ra";

                        novelItems.Add(novelItem);

                        if (--novelCountDown == 0)
                        {
                            break;
                        }
                    }
                    crawlPosition = 0;
                }
            }

            // Calculate total novels
            url = $"https://truyenfull.vn/the-loai/{genre}/trang-{totalCrawlPages}"; ;
            doc = web.Load(url);
            int totalNovels = 0;

            HtmlNodeCollection lastPageNodes = doc.DocumentNode.SelectNodes("//div[@class='row' and @itemscope and @itemtype='https://schema.org/Book']");
            if (lastPageNodes != null)
            {
                int novelOflastCrawedPage = doc.DocumentNode.SelectNodes("//div[@class='row' and @itemscope and @itemtype='https://schema.org/Book']").Count();
                totalNovels = (totalCrawlPages - 1) * maxPerCrawlPage + novelOflastCrawedPage;
            }

            if (totalNovels > 5)
            {
                totalNovels--;
            }

            // Return object
            NovelSearchResult response = new NovelSearchResult();
            response.Page = page;
            response.PerPage = perPage;
            response.Total = totalNovels;
            response.TotalPages = totalNovels / perPage + (totalNovels % perPage == 0 ? 0 : 1);
            response.Data = novelItems;

            return response;
        }

        public IEnumerable<Genre> GetGenres()
        {
            return null;
        }

        public NovelDetail GetNovelDetail(string novelUrl)
        {
            return null;
        }
    }
}
