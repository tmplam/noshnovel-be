using HtmlAgilityPack;
using NoshNovel.Models;
using NoshNovel.Plugins;
using NoshNovel.Plugins.Attributes;

namespace NoshNovel.Servers.TruyenChu
{
    [NovelServer("truyenchu.com.vn")]
    public partial class TruyenChuCrawler : INovelCrawler
    {
        public NovelSearchResult FilterByGenre(string genre, int page = 1, int perPage = 18)
        {
            // Calculate page and position to crawl
            int startPosition = (page - 1) * perPage + 1;
            int firstCrawledPage = startPosition / maxPerCrawlPage + 1;
            int crawlPosition = startPosition % maxPerCrawlPage - 1;

            HtmlWeb web = new HtmlWeb();
            string url = $"{baseUrl}/{genre}?page=1";
            HtmlDocument doc = web.Load(url);

            // Calculate total crawled pages
            int totalNovels = 0;
            HtmlNode totalItemNode = doc.DocumentNode.SelectSingleNode(".//p[@class='text-sm']");
            if (totalItemNode != null)
            {
                string totalNovelString = totalItemNode.SelectNodes(".//span[@class='font-medium']").ElementAt(2).InnerText;
                int.TryParse(totalNovelString.Replace(".", ""), out totalNovels);
            }
            int totalCrawlPages = totalNovels / maxPerCrawlPage + (totalNovels % maxPerCrawlPage == 0 ? 0 : 1);


            // Crawl novel and add to list
            int novelCountDown = perPage;
            List<NovelItem> novelItems = new List<NovelItem>();

            for (int i = firstCrawledPage; i <= totalCrawlPages && novelCountDown > 0; i++)
            {
                url = $"{baseUrl}/{genre}?page={i}";
                doc = web.Load(url);

                var novelNodes = doc.DocumentNode.SelectNodes("//article");

                if (novelNodes != null)
                {
                    for (int j = crawlPosition; j < novelNodes.Count(); j++)
                    {
                        NovelItem novelItem = new NovelItem();

                        // Get information from tag
                        HtmlNode novelNode = novelNodes[j];

                        novelItem.Description = novelNode.SelectSingleNode(".//summary").InnerText;

                        HtmlNode titleNode = novelNode.SelectSingleNode(".//a[@itemprop='url']");

                        novelItem.Title = titleNode.InnerText.Trim();
                        string[] hrefTokens = titleNode.GetAttributeValue("href", "").Split("/", StringSplitOptions.TrimEntries);
                        novelItem.NovelSlug = hrefTokens[hrefTokens.Length - 1];

                        string[] chapterTokens = novelNode.SelectSingleNode(".//span[@class='line-clamp-1 text-sm']").InnerText.Trim().Split();

                        try
                        {
                            novelItem.TotalChapter = int.Parse(chapterTokens[0]);
                        }
                        catch (Exception)
                        {
                            novelItem.TotalChapter = 0;
                        }

                        HtmlNode genreNode = novelNode.SelectSingleNode(".//a[@class='line-clamp-1 text-right text-sm !text-my_green hover:underline dark:!text-my_green']");

                        List<Genre> genreList = new List<Genre>()
                        {
                            new Genre()
                            {
                                Name = genreNode.InnerText.Trim(),
                                Slug = genreNode.GetAttributeValue("href", string.Empty).Trim('/')
                            }
                        };
                        novelItem.Genres = genreList;

                        // Take image
                        HtmlNode noscriptNode = novelNode.SelectSingleNode(".//noscript");
                        if (noscriptNode != null)
                        {
                            HtmlNode imgNode = noscriptNode.SelectSingleNode(".//img");
                            if (imgNode != null)
                            {
                                // /images/no-image.webp
                                string imageUrl = imgNode.GetAttributeValue("src", "").Trim().Split('?')[0];
                                if (imageUrl == "/images/no-image.webp")
                                {
                                    imageUrl = "https://truyenchu.com.vn/images/no-image.webp";
                                }
                                novelItem.CoverImage = imageUrl;
                            }
                        }

                        novelItems.Add(novelItem);

                        if (--novelCountDown == 0)
                        {
                            break;
                        }
                    }
                    crawlPosition = 0;
                }
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

        public NovelSearchResult GetByKeyword(string keyword, int page = 1, int perPage = 18)
        {
            // Calculate page and position to crawl
            int startPosition = (page - 1) * perPage + 1;
            int firstCrawledPage = startPosition / maxPerCrawlPage + 1;
            int crawlPosition = startPosition % maxPerCrawlPage - 1;

            keyword = string.Join("+", keyword.Split(" ", StringSplitOptions.RemoveEmptyEntries));

            HtmlWeb web = new HtmlWeb();
            string url = $"{baseUrl}/tim-kiem/?keyword={keyword}&page=1";
            HtmlDocument doc = web.Load(url);

            // Calculate total crawled pages
            int totalNovels = 0;
            HtmlNode totalItemNode = doc.DocumentNode.SelectSingleNode(".//p[@class='text-sm']");
            if (totalItemNode != null)
            {
                string totalNovelString = totalItemNode.SelectNodes(".//span[@class='font-medium']").ElementAt(2).InnerText;
                int.TryParse(totalNovelString.Replace(".", ""), out totalNovels);
            }
            int totalCrawlPages = totalNovels / maxPerCrawlPage + (totalNovels % maxPerCrawlPage == 0 ? 0 : 1);


            // Crawl novel and add to list
            int novelCountDown = perPage;
            List<NovelItem> novelItems = new List<NovelItem>();

            for (int i = firstCrawledPage; i <= totalCrawlPages && novelCountDown > 0; i++)
            {
                url = $"{baseUrl}/tim-kiem?keyword={keyword}&page={i}";
                doc = web.Load(url);

                var novelNodes = doc.DocumentNode.SelectNodes("//article");

                if (novelNodes != null)
                {
                    for (int j = crawlPosition; j < novelNodes.Count(); j++)
                    {
                        NovelItem novelItem = new NovelItem();

                        // Get information from tag
                        HtmlNode novelNode = novelNodes[j];

                        novelItem.Description = novelNode.SelectSingleNode(".//summary").InnerText;

                        HtmlNode titleNode = novelNode.SelectSingleNode(".//a[@itemprop='url']");

                        novelItem.Title = titleNode.InnerText.Trim();
                        string[] hrefTokens = titleNode.GetAttributeValue("href", "").Split("/", StringSplitOptions.TrimEntries);
                        novelItem.NovelSlug = hrefTokens[hrefTokens.Length - 1];

                        string[] chapterTokens = novelNode.SelectSingleNode(".//span[@class='line-clamp-1 text-sm']").InnerText.Trim().Split();

                        try
                        {
                            novelItem.TotalChapter = int.Parse(chapterTokens[0]);
                        }
                        catch (Exception)
                        {
                            novelItem.TotalChapter = 0;
                        }

                        HtmlNode genreNode = novelNode.SelectSingleNode(".//a[@class='line-clamp-1 text-right text-sm !text-my_green hover:underline dark:!text-my_green']");
                        
                        List<Genre> genreList = new List<Genre>()
                        {
                            new Genre()
                            {
                                Name = genreNode.InnerText.Trim(),
                                Slug = genreNode.GetAttributeValue("href", string.Empty).Trim('/')
                            }
                        };
                        novelItem.Genres = genreList;

                        // Take image
                        HtmlNode noscriptNode = novelNode.SelectSingleNode(".//noscript");
                        if (noscriptNode != null)
                        {
                            HtmlNode imgNode = noscriptNode.SelectSingleNode(".//img");
                            if (imgNode != null)
                            {
                                // /images/no-image.webp
                                string imageUrl = imgNode.GetAttributeValue("src", "").Trim().Split('?')[0];
                                if (imageUrl == "/images/no-image.webp")
                                {
                                    imageUrl = "https://truyenchu.com.vn/images/no-image.webp";
                                }
                                novelItem.CoverImage = imageUrl;
                            }
                        }

                        novelItems.Add(novelItem);

                        if (--novelCountDown == 0)
                        {
                            break;
                        }
                    }
                    crawlPosition = 0;
                }
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

        public NovelChaptersResult GetChapterList(string novelSlug, int page = 1, int perPage = 40)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Genre> GetGenres()
        {
            HtmlWeb web = new HtmlWeb();
            var doc = web.Load(baseUrl);

            var genreListNode = doc.DocumentNode.SelectSingleNode("//div[@class='hidden grid grid-cols-1 py-2 sm:grid-cols-2']");

            List<Genre> genres = new List<Genre>();
            if (genreListNode != null)
            {
                var genreNodes = genreListNode.SelectNodes(".//a");
                foreach (var genreNode in genreNodes)
                {
                    Genre genre = new Genre()
                    {
                        Name = genreNode.InnerText.Trim(),
                        Slug = genreNode.GetAttributeValue("href", string.Empty).Trim('/')
                    };
                    genres.Add(genre);
                }
            }
            return genres;
        }

        public NovelContent GetNovelContent(string novelSlug, string chapterSlug)
        {
            throw new NotImplementedException();
        }

        public NovelDetail GetNovelDetail(string novelSlug)
        {
            throw new NotImplementedException();
        }
    }
}
