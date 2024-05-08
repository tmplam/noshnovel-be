using HtmlAgilityPack;
using NoshNovel.Models;
using NoshNovel.Plugins;
using NoshNovel.Plugins.Attributes;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

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
                        novelItem.NovelSlug = hrefTokens[hrefTokens.Length - 1].Replace(".html", "");

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
                        novelItem.NovelSlug = hrefTokens[hrefTokens.Length - 1].Replace(".html", "");

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
            NovelChaptersResult response = new NovelChaptersResult();
            response.Page = page;
            response.PerPage = perPage;

            // Calculate page and position to crawl
            int startPosition = (page - 1) * perPage + 1;
            int firstCrawledPage = startPosition / maxPerCrawledChaptersPage + 1;
            int crawlPosition = startPosition % maxPerCrawledChaptersPage - 1;

            HtmlWeb web = new HtmlWeb();
            var novelUrl = $"{baseUrl}/truyen/{novelSlug}";
            HtmlDocument doc = web.Load(novelUrl);

            var script = doc.DocumentNode.SelectSingleNode("//script[contains(@id,'__NEXT_DATA__')]");
            string jsonString = script.InnerText;
            // Convert json string to object
            JObject jsonObject = JObject.Parse(jsonString);
            JToken? pageProps = jsonObject["props"]?["pageProps"];

            int totalChapters;
            int.TryParse(pageProps?["total"]?.ToString(), out totalChapters);

            int totalCrawlPages = totalChapters / maxPerCrawledChaptersPage + (totalChapters % maxPerCrawledChaptersPage == 0 ? 0 : 1);

            int chapterCountDown = perPage;

            List<Chapter> chapters = new List<Chapter>();
            for (int i = firstCrawledPage; i <= totalCrawlPages && chapterCountDown > 0; i++)
            {
                var url = $"{novelUrl}?page={i}";
                doc = web.Load(url);

                script = doc.DocumentNode.SelectSingleNode("//script[contains(@id,'__NEXT_DATA__')]");
                jsonString = script.InnerText;
                // Convert json string to object
                jsonObject = JObject.Parse(jsonString);
                pageProps = jsonObject["props"]?["pageProps"];
                JToken? chapterListToken = pageProps?["chapterList"];

                if (chapterListToken != null)
                {
                    // Convert JToken to list
                    List<CrawlChapter>? crawlChapters = chapterListToken.ToObject<List<CrawlChapter>>();

                    // Duyệt qua danh sách hoặc mảng và in thông tin
                    if (crawlChapters != null)
                    {
                        for (int j = crawlPosition; j < crawlChapters.Count(); j++)
                        {
                            var crawlChapter = crawlChapters[j];
                            Chapter chapter = new Chapter()
                            {
                                Label = $"Chương {(i - 1) * 50 + j + 1}",
                                Name = crawlChapter.name,
                                Slug = crawlChapter.slug,
                            };
                            chapters.Add(chapter);

                            if (--chapterCountDown == 0)
                            {
                                break;
                            }
                        }
                        crawlPosition = 0;
                    }
                }
            }

            response.Total = totalChapters;
            response.TotalPages = totalChapters / perPage + (totalChapters % perPage == 0 ? 0 : 1);
            response.Data = chapters;

            return response;
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
            var novelContent = new NovelContent();

            HtmlWeb web = new HtmlWeb();
            HtmlDocument doc = web.Load($"{baseUrl}/truyen/{novelSlug}/{chapterSlug}");


            var headerNode = doc.DocumentNode.SelectSingleNode(".//header[@class='chapter-bg-color chapter-content-size mx-auto']");
            novelContent.Title = headerNode.SelectSingleNode(".//h1").InnerText.Trim();

            string chapterString = headerNode.SelectSingleNode(".//h2").InnerText.Trim();

            novelContent.Chapter = new Chapter()
            {
                Label = chapterString,
                Name = string.Empty,
                Slug = chapterSlug
            };

            novelContent.Content = doc.DocumentNode.SelectSingleNode(".//div[@class='chapter-content']").InnerHtml;
            novelContent.Content = Regex.Replace(novelContent.Content, "<script[^>]*>.*?</script>", "");

            return novelContent;
        }

        public NovelDetail GetNovelDetail(string novelSlug)
        {
            NovelDetail novel = new NovelDetail();

            HtmlWeb web = new HtmlWeb();
            HtmlDocument doc = web.Load($"{baseUrl}/truyen/{novelSlug}");

            HtmlNode coverImageNode = doc.DocumentNode.SelectSingleNode(".//img[@class='w-full object-cover object-center']");
            novel.CoverImage = coverImageNode.GetAttributeValue("src", string.Empty);

            HtmlNode infoNode = doc.DocumentNode.SelectSingleNode(".//div[@class='py-5 md:px-5 md:py-0 flex flex-col grow']");

            novel.Title = infoNode.SelectSingleNode(".//h1[@itemprop='name']").InnerText.Trim();

            HtmlNode authorNode = infoNode.SelectSingleNode(".//a[@itemprop='author']");
            string[] authorTokens = authorNode.GetAttributeValue("href", "").Split('/', StringSplitOptions.RemoveEmptyEntries);

            novel.Author = new Author()
            {
                Name = authorNode.InnerText.Trim(),
                Slug = authorTokens[authorTokens.Length - 1]
            };

            var genreNodes = infoNode.SelectNodes(".//a[@itemprop='genre']");

            List<Genre> genres = new List<Genre>();
            foreach (var genreNode in genreNodes)
            {
                Genre genre = new Genre()
                {
                    Name = genreNode.InnerText.Trim(),
                    Slug = genreNode.GetAttributeValue("href", string.Empty).Trim('/')
                };
                genres.Add(genre);
            }
            novel.Genres = genres;

            var statusNode = infoNode.SelectNodes(".//div[@class='mb-2 flex items-center']")[1].ChildNodes[1];
            novel.Status = statusNode.InnerText.Trim();

            HtmlNode ratingNode = doc.DocumentNode.SelectSingleNode(".//span[@itemprop='ratingValue']");
            HtmlNode reviewsNumberNode = doc.DocumentNode.SelectSingleNode(".//span[@itemprop='ratingCount']");
            try
            {
                novel.Rating = double.Parse(ratingNode.InnerText.Trim());
                novel.ReviewsNumber = int.Parse(reviewsNumberNode.InnerText.Trim());
            }
            catch (Exception)
            {
                novel.Rating = 0;
                novel.ReviewsNumber = 0;
            }

            string intro = doc.DocumentNode.SelectSingleNode(".//div[@id='bookIntro']").InnerHtml;
            novel.Description = Regex.Replace(intro, @"href=""[^""]*""", "href=\"#\"");

            return novel;
        }
    }
}
