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
        private static readonly string baseUrl = "https://truyenfull.vn";
        // Number of maximum novels per search page of crawled page
        private static readonly int maxPerCrawlPage = 27;
        // Number of maximum chapters per detail page of crawled detail page
        private static readonly int maxPerCrawledChaptersPage = 50;

        public NovelSearchResult GetByKeyword(string keyword, int page = 1, int perPage = 18)
        {
            // Calculate page and position to crawl
            int startPosition = (page - 1) * perPage + 1;
            int firstCrawledPage = startPosition / maxPerCrawlPage + 1;
            int crawlPosition = startPosition % maxPerCrawlPage - 1;

            keyword = string.Join("%20", keyword.Split(" ", StringSplitOptions.RemoveEmptyEntries));

            // Calculate total crawled pages
            HtmlWeb web = new HtmlWeb();
            string url = $"{baseUrl}/tim-kiem/?tukhoa={keyword}";
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
                url = $"{baseUrl}/tim-kiem/?tukhoa={keyword}&page={i}";
                doc = web.Load(url);

                HtmlNodeCollection novelNodes = doc.DocumentNode.SelectNodes("//div[@class='row' and @itemscope and @itemtype='https://schema.org/Book']");

                if (novelNodes != null)
                {
                    for (int j = crawlPosition; j < novelNodes.Count(); j++)
                    {
                        NovelItem novelItem = new NovelItem();

                        HtmlNode novelNode = novelNodes[j];

                        novelItem.CoverImage = novelNode.SelectSingleNode(".//div[@class='lazyimg']").GetAttributeValue("data-image", "");
                        novelItem.Author = new Author()
                        {
                            Name = novelNode.SelectSingleNode(".//span[@class='author']").InnerText.Trim()
                        };
                        novelItem.NovelSlug = novelNode.SelectSingleNode(".//a[@itemprop='url']")
                            .GetAttributeValue("href", "").Split("/", StringSplitOptions.RemoveEmptyEntries)[2];
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
            url = $"{baseUrl}/tim-kiem/?tukhoa={keyword}&page={totalCrawlPages}";
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
            string url = $"{baseUrl}/the-loai/{genre}/";
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
                url = $"{baseUrl}/the-loai/{genre}/trang-{i}";
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
                        novelItem.Author = new Author()
                        {
                            Name = novelNode.SelectSingleNode(".//span[@class='author']").InnerText.Trim()
                        };
                        novelItem.NovelSlug = novelNode.SelectSingleNode(".//a[@itemprop='url']")
                            .GetAttributeValue("href", "").Split("/", StringSplitOptions.RemoveEmptyEntries)[2];
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
            var url = baseUrl;

            HtmlWeb web = new HtmlWeb();
            HtmlDocument doc = web.Load(url);

            HtmlNode navNode = doc.DocumentNode.SelectSingleNode("//ul[contains(@class, 'control') and contains(@class, 'nav') and contains(@class, 'navbar-nav')]");

            List<Genre> genres = new List<Genre>();
            if (navNode != null)
            {
                // Select the genre node of nav and go to its content
                HtmlNodeCollection genreWrappers = navNode.SelectNodes("./li")[1].SelectSingleNode("div").SelectSingleNode("div").SelectNodes("div");

                foreach (var genreWrapper in genreWrappers)
                {
                    HtmlNodeCollection genreList = genreWrapper.SelectSingleNode("ul").SelectNodes("li");
                    foreach (var genreItem in genreList)
                    {
                        HtmlNode genreLinkNode = genreItem.SelectSingleNode("a");
                        Genre genre = new Genre()
                        {
                            Name = genreLinkNode.InnerText.Trim(),
                            Slug = genreLinkNode.GetAttributeValue("href", "").Split("/", StringSplitOptions.RemoveEmptyEntries)[3]
                        };
                        genres.Add(genre);
                    }
                }
            }
            return genres;
        }

        public NovelDetail GetNovelDetail(string novelSlug)
        {
            NovelDetail novel = new NovelDetail();

            HtmlWeb web = new HtmlWeb();
            HtmlDocument doc = web.Load($"{baseUrl}/{novelSlug}");

            novel.Title = doc.DocumentNode.SelectSingleNode("//h3[@class='title' and @itemprop='name']").InnerText.Trim();
            novel.CoverImage = doc.DocumentNode.SelectSingleNode("//img[@itemprop='image']").GetAttributeValue("src", "");

            HtmlNode infoNode = doc.DocumentNode.SelectSingleNode("//div[@class='info']");

            novel.Author = new Author()
            {
                Name = infoNode.SelectSingleNode(".//a[@itemprop='author']").InnerText,
                Slug = infoNode.SelectSingleNode(".//a[@itemprop='author']").GetAttributeValue("href", "")
                    .Split("/", StringSplitOptions.RemoveEmptyEntries)[3]
            };

            HtmlNodeCollection genreNodes = infoNode.SelectNodes(".//a[@itemprop='genre']");
            List<Genre> genreList = new List<Genre>();
            foreach (HtmlNode genreNode in genreNodes)
            {
                Genre genre = new Genre()
                {
                    Name = genreNode.InnerText.Trim(),
                    Slug = genreNode.GetAttributeValue("href", "").Split("/", StringSplitOptions.RemoveEmptyEntries)[3]
                };
                genreList.Add(genre);
            }
            novel.Genres = genreList;
            novel.Status = infoNode.SelectNodes("div")[2].SelectSingleNode("span").InnerText.Trim();
            novel.Rating = double.Parse(doc.DocumentNode.SelectSingleNode("//span[@itemprop='ratingValue']").InnerText) / 2;
            novel.Description = doc.DocumentNode.SelectSingleNode("//div[@itemprop='description']").InnerHtml;

            return novel;
        }

        public NovelChaptersResult GetChapterList(string novelSlug, int page = 1, int perPage = 40)
        {
            // Calculate page and position to crawl
            int startPosition = (page - 1) * perPage + 1;
            int firstCrawledPage = startPosition / maxPerCrawledChaptersPage + 1;
            int crawlPosition = startPosition % maxPerCrawledChaptersPage - 1;

            HtmlWeb web = new HtmlWeb();
            var novelUrl = $"{baseUrl}/{novelSlug}";
            HtmlDocument doc = web.Load(novelUrl);

            int totalCrawlPages = int.Parse(doc.DocumentNode.SelectSingleNode("//input[@id='total-page']").GetAttributeValue("value", "1"));

            int chapterCountDown = perPage;

            List<Chapter> chapters = new List<Chapter>();
            for (int i = firstCrawledPage; i <= totalCrawlPages && chapterCountDown > 0; i++)
            {
                var url = $"{novelUrl}/trang-{i}";
                doc = web.Load(url);

                HtmlNodeCollection chapterWrappers = doc.DocumentNode.SelectNodes("//ul[@class='list-chapter']");

                if (chapterWrappers != null)
                {
                    var chapterList = chapterWrappers.SelectMany(chapterWrapper => chapterWrapper.Descendants("a")).ToList();
                    for (int j = crawlPosition; j < chapterList.Count(); j++)
                    {
                        HtmlNode chapterNode = chapterList[j];
                        string[] chapterTokens = chapterNode.InnerText.Split(":", 2, StringSplitOptions.RemoveEmptyEntries);

                        Chapter chapter = new Chapter();
                        chapter.ChapterNumber = int.Parse(chapterTokens[0].Split(" ", StringSplitOptions.RemoveEmptyEntries)[1]);

                        if (chapterTokens.Length > 1)
                        {
                            chapter.Name = chapterTokens[1].Trim().Capitalize();
                        }
                        chapters.Add(chapter);

                        if (--chapterCountDown == 0)
                        {
                            break;
                        }
                    }
                    crawlPosition = 0;
                }
            }

            // Calculate total chapters
            doc = web.Load($"{novelUrl}/trang-{totalCrawlPages}");
            int totalChapters = 0;

            HtmlNodeCollection last = doc.DocumentNode.SelectNodes("//ul[@class='list-chapter']");
            if (last != null)
            {
                int novelOflastCrawedPage = last.SelectMany(chapterWrapper => chapterWrapper.Descendants("a")).Count();
                totalChapters = (totalCrawlPages - 1) * maxPerCrawledChaptersPage + novelOflastCrawedPage;
            }

            NovelChaptersResult response = new NovelChaptersResult();
            response.Page = page;
            response.PerPage = perPage;
            response.Total = totalChapters;
            response.TotalPages = totalChapters / perPage + (totalChapters % perPage == 0 ? 0 : 1);
            response.Data = chapters;

            return response;
        }
    }
}
