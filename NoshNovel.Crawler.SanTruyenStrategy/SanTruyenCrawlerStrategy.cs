using HtmlAgilityPack;
using NoshNovel.Models;
using NoshNovel.Plugin.Strategies;
using NoshNovel.Plugin.Strategies.Attributes;
using NoshNovel.Plugin.Strategies.Exeptions;
using NoshNovel.Plugin.Strategies.Utilities;
using System.Net;

namespace NoshNovel.Server.SanTruyenStrategy
{
    [NovelServer("santruyen.com")]
    public partial class SanTruyenCrawlerStrategy : INovelCrawlerStrategy
    {
        public async Task<NovelSearchResult> FilterByGenre(string genre, int page = 1, int perPage = 18)
        {
            int startPosition = (page - 1) * perPage + 1;
            int firstCrawledPage = startPosition / maxPerCrawlPage + (startPosition % maxPerCrawlPage == 0 ? 0 : 1);
            int crawlPosition = (startPosition - 1) % maxPerCrawlPage;

            genre = HelperClass.GenerateSlug(genre);
            string url = $"{baseUrl}/{genre}";

            NovelSearchResult searchResult = new NovelSearchResult();
            searchResult.Page = page;
            searchResult.PerPage = perPage;

            using (HttpClient httpClient = new HttpClient())
            {
                // make request
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
                HttpResponseMessage response = await httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    throw new RequestExeption(HttpStatusCode.NotFound, "Genre not found in crawled server");
                }

                string responseContent = await response.Content.ReadAsStringAsync();
                // decodes html-encoded
                responseContent = WebUtility.HtmlDecode(responseContent);
                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(responseContent);

                HtmlNodeCollection paginationElementNodes = doc.DocumentNode.SelectNodes("//ul[@class='pagination']/li[not(contains(@class, 'dropup')) and not(descendant::span[contains(@class, 'glyphicon')])]/a");
                HtmlNode? lastPaginationElementNode = null;

                if (paginationElementNodes != null && paginationElementNodes.Count != 0)
                {
                    lastPaginationElementNode = paginationElementNodes[^1];
                }

                int totalCrawlPages = 1;

                if (lastPaginationElementNode != null)
                {
                    var hrefParts = lastPaginationElementNode.GetAttributeValue("href", "").Split("/", StringSplitOptions.RemoveEmptyEntries);
                    totalCrawlPages = int.Parse(hrefParts[3].Split("-")[1]);
                }

                int novelCountDown = perPage;
                List<NovelItem> novelItems = new List<NovelItem>();

                for (int i = firstCrawledPage; i <= totalCrawlPages && novelCountDown > 0; i++)
                {
                    if (i > 1)
                    {
                        // Make more requests
                        url = $"{baseUrl}/{genre}/trang-{i}";
                        request = new HttpRequestMessage(HttpMethod.Get, url);
                        response = await httpClient.SendAsync(request);

                        if (!response.IsSuccessStatusCode)
                        {
                            throw new RequestExeption(HttpStatusCode.NotFound, "No content found in crawled server");
                        }

                        responseContent = response.Content.ReadAsStringAsync().Result;
                        // Decodes html-encoded
                        responseContent = WebUtility.HtmlDecode(responseContent);
                        doc.LoadHtml(responseContent);
                    }

                    HtmlNodeCollection novelNodes = doc.DocumentNode.SelectNodes("//div[@class='stories']/div[@class='story-box']");

                    if (novelNodes == null)
                    {
                        continue;
                    }

                    for (int j = crawlPosition; j < novelNodes.Count; j++)
                    {
                        NovelItem novelItem = new NovelItem();

                        HtmlNode novelNode = novelNodes[j];

                        novelItem.Title = novelNode.SelectSingleNode("./div[@class='txt']/h3[@class='story-title']/a").InnerText.Trim();

                        // Author
                        HtmlNode authorNode = novelNode.SelectSingleNode("./div[@class='txt']/p[@itemprop='author']");

                        if (authorNode != null)
                        {
                            novelItem.Author = new Author()
                            {
                                Name = authorNode.InnerText.Trim(),
                                Slug = ""
                            };
                        }

                        // Genre
                        HtmlNodeCollection genreNodes = novelNode.SelectNodes("./div[@class='txt']/p[@class='story-genres']/a");

                        if (genreNodes != null)
                        {
                            List<Genre> genres = new List<Genre>();

                            foreach (var genreNode in genreNodes)
                            {
                                Genre novelGenre = new Genre()
                                {
                                    Name = genreNode.InnerText.Trim(),
                                    Slug = genreNode.GetAttributeValue("href", "").Split("/", StringSplitOptions.RemoveEmptyEntries)[2].Trim()
                                };

                                genres.Add(novelGenre);
                            }

                            novelItem.Genres = genres;
                        }

                        novelItem.CoverImage = novelNode.SelectSingleNode("./a[@class='cover']/noscript/img").GetAttributeValue("src", "").Trim();
                        novelItem.NovelSlug = novelNode.SelectSingleNode("./a[@class='cover']").GetAttributeValue("href", "").Split("/", StringSplitOptions.RemoveEmptyEntries)[2].Trim();

                        HtmlNode statusNode = novelNode.SelectSingleNode("./div[@class='txt']/p[not(contains(@class, 'story-genres')) and not(contains(@itemprop, 'author'))]/label");

                        if (statusNode.GetAttributeValue("class", "").Trim() == "full-1")
                        {
                            novelItem.Status = "Full";
                        }
                        else
                        {
                            novelItem.Status = "Đang ra";
                        }

                        novelItem.TotalChapter = int.Parse(novelNode.SelectSingleNode("./div[@class='txt']/p/span[@class='count-chapter']").InnerText.Trim().Split(" ")[0]);

                        novelItems.Add(novelItem);

                        if (--novelCountDown == 0)
                        {
                            break;
                        }
                    }

                    crawlPosition = 0;
                }

                searchResult.Data = novelItems;

                if (totalCrawlPages == 1)
                {
                    url = $"{baseUrl}/{genre}/trang-{totalCrawlPages}/";
                }
                else
                {
                    url = $"{baseUrl}/{genre}/trang-{totalCrawlPages}";
                }

                Console.WriteLine(url);
                // Calculate total novels
                request = new HttpRequestMessage(HttpMethod.Get, url);
                response = httpClient.Send(request);

                if (response.IsSuccessStatusCode)
                {
                    responseContent = await response.Content.ReadAsStringAsync();
                    // Decodes html-encoded
                    responseContent = WebUtility.HtmlDecode(responseContent);
                    doc.LoadHtml(responseContent);

                    HtmlNodeCollection lastPageNodes = doc.DocumentNode.SelectNodes("//div[@class='stories']/div[@class='story-box']");

                    int novelOflastCrawedPage = 0;

                    if (lastPageNodes != null)
                    {
                        novelOflastCrawedPage = lastPageNodes.Count;
                    }

                    int totalNovels = (totalCrawlPages - 1) * maxPerCrawlPage + novelOflastCrawedPage;

                    searchResult.Total = totalNovels;
                    searchResult.TotalPages = totalNovels / perPage + (totalNovels % perPage == 0 ? 0 : 1);
                }
            }

            return searchResult;
        }

        public async Task<NovelSearchResult> GetByKeyword(string keyword, int page = 1, int perPage = 18)
        {
            int startPosition = (page - 1) * perPage + 1;
            int firstCrawledPage = startPosition / maxPerCrawlPage + (startPosition % maxPerCrawlPage == 0 ? 0 : 1);
            int crawlPosition = (startPosition - 1) % maxPerCrawlPage;

            string url = $"{baseUrl}/tim-kiem/?tukhoa={keyword}";

            NovelSearchResult searchResult = new NovelSearchResult();
            searchResult.Page = page;
            searchResult.PerPage = perPage;

            using (HttpClient httpClient = new HttpClient())
            {
                // make request
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
                HttpResponseMessage response = await httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    throw new RequestExeption(HttpStatusCode.NotFound, "No content found in crawled server");
                }

                string responseContent = await response.Content.ReadAsStringAsync();
                // decodes html-encoded
                responseContent = WebUtility.HtmlDecode(responseContent);
                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(responseContent);

                HtmlNodeCollection paginationElementNodes = doc.DocumentNode.SelectNodes("//ul[@class='pagination']/li[not(contains(@class, 'dropup')) and not(descendant::span[contains(@class, 'glyphicon')])]/a");
                HtmlNode? lastPaginationElementNode = null;

                if (paginationElementNodes != null && paginationElementNodes.Count != 0)
                {
                    lastPaginationElementNode = paginationElementNodes[^1];
                }
                int totalCrawlPages = 1;

                if (lastPaginationElementNode != null)
                {
                    var hrefParts = lastPaginationElementNode.GetAttributeValue("href", "").Split("/", StringSplitOptions.RemoveEmptyEntries);
                    totalCrawlPages = int.Parse(hrefParts[3].Split("=")[2]);
                }

                int novelCountDown = perPage;
                List<NovelItem> novelItems = new List<NovelItem>();

                for (int i = firstCrawledPage; i <= totalCrawlPages && novelCountDown > 0; i++)
                {
                    if (i > 1)
                    {
                        // Make more requests
                        url = $"{baseUrl}/tim-kiem/?tukhoa={keyword}&paged={i}";
                        request = new HttpRequestMessage(HttpMethod.Get, url);
                        response = await httpClient.SendAsync(request);

                        if (!response.IsSuccessStatusCode)
                        {
                            throw new RequestExeption(HttpStatusCode.NotFound, "No content found in crawled server");
                        }

                        responseContent = response.Content.ReadAsStringAsync().Result;
                        // Decodes html-encoded
                        responseContent = WebUtility.HtmlDecode(responseContent);
                        doc.LoadHtml(responseContent);
                    }

                    HtmlNodeCollection novelNodes = doc.DocumentNode.SelectNodes("//div[@class='stories']/div[@class='story-box']");

                    if (novelNodes == null)
                    {
                        continue;
                    }

                    for (int j = crawlPosition; j < novelNodes.Count; j++)
                    {
                        NovelItem novelItem = new NovelItem();

                        HtmlNode novelNode = novelNodes[j];

                        novelItem.Title = novelNode.SelectSingleNode("./div[@class='txt']/h3[@class='story-title']/a").InnerText.Trim();

                        // Author
                        HtmlNode authorNode = novelNode.SelectSingleNode("./div[@class='txt']/p[@itemprop='author']");

                        if (authorNode != null)
                        {
                            novelItem.Author = new Author()
                            {
                                Name = authorNode.InnerText.Trim(),
                                Slug = ""
                            };
                        }

                        // Genre
                        HtmlNodeCollection genreNodes = novelNode.SelectNodes("./div[@class='txt']/p[@class='story-genres']/a");

                        if (genreNodes != null)
                        {
                            List<Genre> genres = new List<Genre>();

                            foreach (var genreNode in genreNodes)
                            {
                                Genre novelGenre = new Genre()
                                {
                                    Name = genreNode.InnerText.Trim(),
                                    Slug = genreNode.GetAttributeValue("href", "").Split("/", StringSplitOptions.RemoveEmptyEntries)[2].Trim()
                                };

                                genres.Add(novelGenre);
                            }

                            novelItem.Genres = genres;
                        }

                        novelItem.CoverImage = novelNode.SelectSingleNode("./a[@class='cover']/noscript/img").GetAttributeValue("src", "").Trim();
                        novelItem.NovelSlug = novelNode.SelectSingleNode("./a[@class='cover']").GetAttributeValue("href", "").Split("/", StringSplitOptions.RemoveEmptyEntries)[2].Trim();

                        HtmlNode statusNode = novelNode.SelectSingleNode("./div[@class='txt']/p[not(contains(@class, 'story-genres')) and not(contains(@itemprop, 'author'))]/label");

                        if (statusNode.GetAttributeValue("class", "").Trim() == "full-1")
                        {
                            novelItem.Status = "Full";
                        }
                        else
                        {
                            novelItem.Status = "Đang ra";
                        }

                        novelItem.TotalChapter = int.Parse(novelNode.SelectSingleNode("./div[@class='txt']/p/span[@class='count-chapter']").InnerText.Trim().Split(" ")[0]);

                        novelItems.Add(novelItem);

                        if (--novelCountDown == 0)
                        {
                            break;
                        }
                    }

                    crawlPosition = 0;
                }

                searchResult.Data = novelItems;

                url = $"{baseUrl}/tim-kiem/?tukhoa={keyword}&paged={totalCrawlPages}/";
                // Calculate total novels
                request = new HttpRequestMessage(HttpMethod.Get, url);
                response = httpClient.Send(request);

                if (response.IsSuccessStatusCode)
                {
                    responseContent = await response.Content.ReadAsStringAsync();
                    // Decodes html-encoded
                    responseContent = WebUtility.HtmlDecode(responseContent);
                    doc.LoadHtml(responseContent);

                    HtmlNodeCollection lastPageNodes = doc.DocumentNode.SelectNodes("//div[@class='stories']/div[@class='story-box']");

                    int novelOflastCrawedPage = 0;

                    if (lastPageNodes != null)
                    {
                        novelOflastCrawedPage = lastPageNodes.Count;
                    }

                    int totalNovels = (totalCrawlPages - 1) * maxPerCrawlPage + novelOflastCrawedPage;

                    searchResult.Total = totalNovels;
                    searchResult.TotalPages = totalNovels / perPage + (totalNovels % perPage == 0 ? 0 : 1);
                }
            }

            return searchResult;
        }

        public async Task<NovelChaptersResult> GetChapterList(string novelSlug, int page = 1, int perPage = 40)
        {
            int startPosition = (page - 1) * perPage + 1;
            int firstCrawledPage = startPosition / maxPerCrawledChaptersPage + (startPosition % maxPerCrawledChaptersPage == 0 ? 0 : 1);
            int crawlPosition = (startPosition - 1) % maxPerCrawledChaptersPage;

            string url = $"{baseUrl}/{novelSlug}";

            NovelChaptersResult chaptersResult = new NovelChaptersResult();
            chaptersResult.Page = page;
            chaptersResult.PerPage = perPage;

            using (HttpClient httpClient = new HttpClient())
            {
                // make request
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
                HttpResponseMessage response = await httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    throw new RequestExeption(HttpStatusCode.NotFound, "Novel not found in crawled server");
                }

                string responseContent = await response.Content.ReadAsStringAsync();
                // decodes html-encoded
                responseContent = WebUtility.HtmlDecode(responseContent);
                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(responseContent);

                HtmlNodeCollection paginationElementNodes = doc.DocumentNode.SelectNodes("//ul[@class='pagination']/li[not(contains(@class, 'dropup')) and not(descendant::span[contains(@class, 'glyphicon')])]/a");
                HtmlNode? lastPaginationElementNode = null;

                if (paginationElementNodes != null && paginationElementNodes.Count != 0)
                {
                    lastPaginationElementNode = paginationElementNodes[^1];
                }

                int totalCrawlPages = 1;

                if (lastPaginationElementNode != null)
                {
                    var hrefParts = lastPaginationElementNode.GetAttributeValue("href", "").Split("/", StringSplitOptions.RemoveEmptyEntries);
                    totalCrawlPages = int.Parse(hrefParts[3].Split("-")[1]);
                }

                int chapterCountDown = perPage;
                List<Chapter> chapterList = new List<Chapter>();

                for (int i = firstCrawledPage; i <= totalCrawlPages && chapterCountDown > 0; i++)
                {
                    if (i > 1)
                    {
                        // Make more requests
                        url = $"{baseUrl}/{novelSlug}/trang-{i}";
                        request = new HttpRequestMessage(HttpMethod.Get, url);
                        response = await httpClient.SendAsync(request);

                        if (!response.IsSuccessStatusCode)
                        {
                            throw new RequestExeption(HttpStatusCode.NotFound, "No content found in crawled server");
                        }

                        responseContent = response.Content.ReadAsStringAsync().Result;
                        // Decodes html-encoded
                        responseContent = WebUtility.HtmlDecode(responseContent);
                        doc.LoadHtml(responseContent);
                    }

                    HtmlNodeCollection chapterListNodes = doc.DocumentNode.SelectNodes("//div[@id='chapter-list']");

                    if (chapterListNodes == null)
                    {
                        continue;
                    }

                    HtmlNodeCollection? chapterNodes = null;

                    if (chapterListNodes.Count == 1)
                    {
                        chapterNodes = chapterListNodes[0].SelectNodes("./ul[contains(@class, 'list-chapter')]/li/a");
                    }
                    else
                    {
                        chapterNodes = chapterListNodes[1].SelectNodes("./ul[contains(@class, 'list-chapter')]/li/a");
                    }

                    if (chapterNodes == null)
                    {
                        continue;
                    }


                    for (int j = crawlPosition; j < chapterNodes.Count; j++)
                    {
                        string chapterLabel = string.Empty;
                        string chapterName = string.Empty;
                        string chapterSlug = string.Empty;

                        HtmlNode chapterNode = chapterNodes[j];

                        chapterSlug = chapterNode.GetAttributeValue("href", "").Split("/", StringSplitOptions.RemoveEmptyEntries)[3].Trim();

                        var chapterTextParts = chapterNode.InnerText.Split(":", StringSplitOptions.RemoveEmptyEntries);
                        chapterLabel = chapterTextParts[0].Trim();

                        if (chapterTextParts.Length > 1)
                        {
                            chapterName = chapterTextParts[^1].Trim();
                        }

                        Chapter chapter = new Chapter()
                        {
                            Name = chapterName,
                            Slug = chapterSlug,
                            Label = chapterLabel,
                            ChapterIndex = startPosition++
                        };

                        chapterList.Add(chapter);

                        if (--chapterCountDown == 0)
                        {
                            break;
                        }
                    }

                    crawlPosition = 0;
                }

                chaptersResult.Data = chapterList;

                url = $"{baseUrl}/{novelSlug}/trang-{totalCrawlPages}/";
                // Calculate total novels
                request = new HttpRequestMessage(HttpMethod.Get, url);
                response = httpClient.Send(request);

                if (response.IsSuccessStatusCode)
                {
                    responseContent = await response.Content.ReadAsStringAsync();
                    // Decodes html-encoded
                    responseContent = WebUtility.HtmlDecode(responseContent);
                    doc.LoadHtml(responseContent);


                    HtmlNodeCollection chapterListNodes = doc.DocumentNode.SelectNodes("//div[@id='chapter-list']");

                    if (chapterListNodes == null)
                    {
                        return chaptersResult;
                    }

                    HtmlNodeCollection? lastPageNodes = null;

                    if (chapterListNodes.Count == 1)
                    {
                        lastPageNodes = chapterListNodes[0].SelectNodes("./ul[contains(@class, 'list-chapter')]/li/a");
                    }
                    else
                    {
                        lastPageNodes = chapterListNodes[1].SelectNodes("./ul[contains(@class, 'list-chapter')]/li/a");
                    }

                    int novelOflastCrawedPage = 0;

                    if (lastPageNodes != null)
                    {
                        novelOflastCrawedPage = lastPageNodes.Count;
                    }

                    int totalChapters = (totalCrawlPages - 1) * maxPerCrawledChaptersPage + novelOflastCrawedPage;

                    chaptersResult.Total = totalChapters;
                    chaptersResult.TotalPages = totalChapters / perPage + (totalChapters % perPage == 0 ? 0 : 1);
                }
            }

            return chaptersResult;
        }

        public async Task<IEnumerable<Genre>> GetGenres()
        {
            var url = baseUrl;
            List<Genre> genres = new List<Genre>();

            using (HttpClient httpClient = new HttpClient())
            {
                // make request
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
                HttpResponseMessage response = await httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    string responseContent = await response.Content.ReadAsStringAsync();
                    // decodes html-encoded
                    responseContent = WebUtility.HtmlDecode(responseContent);
                    HtmlDocument doc = new HtmlDocument();
                    doc.LoadHtml(responseContent);
                    HtmlNodeCollection genreNodes = doc.DocumentNode.SelectNodes("//div[@class='list-genres']/div/a");

                    if (genreNodes != null)
                    {
                        foreach (var genreNode in genreNodes)
                        {
                            var genreSlug = genreNode.GetAttributeValue("href", "").Split("/", StringSplitOptions.RemoveEmptyEntries)[2].Trim();

                            if (genreSlug == "khac")
                            {
                                continue;
                            }

                            var genreName = genreNode.GetAttributeValue("title", "");

                            Genre genre = new Genre()
                            {
                                Slug = genreSlug,
                                Name = genreName
                            };

                            genres.Add(genre);
                        }

                        Genre otherGenre = new Genre()
                        {
                            Slug = "khac",
                            Name = "Khác"
                        };

                        genres.Add(otherGenre);
                    }
                }
                else
                {
                    throw new RequestExeption(HttpStatusCode.NotFound, "No content found in crawled server");
                }
            }

            return genres;
        }

        public async Task<NovelContent> GetNovelContent(string novelSlug, string chapterSlug)
        {
            novelSlug = novelSlug.Split(".")[0].Trim();

            var url = $"{baseUrl}/{novelSlug}/{chapterSlug}";

            NovelContent novelContent = new NovelContent();

            using (HttpClient httpClient = new HttpClient())
            {
                // make request
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
                HttpResponseMessage response = await httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    string responseContent = await response.Content.ReadAsStringAsync();
                    // Decodes html-encoded
                    responseContent = WebUtility.HtmlDecode(responseContent);
                    HtmlDocument doc = new HtmlDocument();
                    doc.LoadHtml(responseContent);

                    HtmlNode titleNode = doc.DocumentNode.SelectSingleNode("//a[@class='story-title']");

                    if (titleNode != null)
                    {
                        novelContent.Title = titleNode.InnerText.Trim();
                    }

                    string chapterLabel = string.Empty;
                    string chapterName = string.Empty;

                    HtmlNode chapterTitlePartsNode = doc.DocumentNode.SelectSingleNode("//a[@class='chapter-title']");

                    if (chapterTitlePartsNode != null)
                    {
                        var chapterTitleParts = chapterTitlePartsNode.GetAttributeValue("title", "").Split("-")[1].Split(":");

                        chapterLabel = chapterTitleParts[0].Trim();

                        if (chapterTitleParts.Length == 2)
                        {
                            chapterName = chapterTitleParts[1].Trim();
                        }
                        else if (chapterTitleParts.Length == 3)
                        {
                            chapterName = chapterTitleParts[2].Trim();
                        }

                        Chapter chapter = new Chapter()
                        {
                            Name = chapterName,
                            Slug = chapterSlug,
                            Label = chapterLabel
                        };

                        novelContent.Chapter = chapter;
                    }


                    HtmlNode contentNode = doc.DocumentNode.SelectSingleNode("//div[@class='chapter-c']");

                    if (contentNode != null)
                    {
                        novelContent.Content = contentNode.InnerHtml;
                    }
                }
                else
                {
                    throw new RequestExeption(HttpStatusCode.NotFound, "No content found in crawled server");
                }
            }
            return novelContent;
        }

        public async Task<NovelDetail> GetNovelDetail(string novelSlug)
        {
            var url = $"{baseUrl}/{novelSlug}";

            NovelDetail novel = new NovelDetail();

            using (HttpClient httpClient = new HttpClient())
            {
                // make request
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
                HttpResponseMessage response = await httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    string responseContent = await response.Content.ReadAsStringAsync();
                    // Decodes html-encoded
                    responseContent = WebUtility.HtmlDecode(responseContent);
                    HtmlDocument doc = new HtmlDocument();
                    doc.LoadHtml(responseContent);

                    HtmlNode novelDetailNode = doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'story-detail')]");

                    if (novelDetailNode == null)
                    {
                        throw new RequestExeption(HttpStatusCode.NotFound, "Novel not found in crawled server");
                    }

                    novel.Title = novelDetailNode.SelectSingleNode("./div[@class='story-info']/h3[@class='title']").InnerText.Trim();
                    novel.Author = new Author()
                    {
                        Name = novelDetailNode.SelectSingleNode("./div[@class='col-left']/div[@class='metas']/div/a[@itemprop='author']").InnerText.Trim(),
                        Slug = ""
                    };

                    HtmlNodeCollection decriptionNodes = novelDetailNode.SelectNodes("./div[@class='story-info']/div[@class='story-desc']/p");

                    for (var i = 0; i < decriptionNodes.Count - 1; i++)
                    {
                        if (i != 0)
                        {
                            novel.Description += " ";
                        }
                        novel.Description += decriptionNodes[i].InnerText.Trim();
                    }

                    novel.Rating = double.Parse(novelDetailNode.SelectSingleNode("./div[@class='story-info']/div[@class='star-rating']/p[@class='score']/span[@itemprop='ratingValue']").InnerText.Trim());
                    novel.ReviewsNumber = int.Parse(novelDetailNode.SelectSingleNode("./div[@class='story-info']/div[@class='star-rating']/p[@class='score']/span[@itemprop='ratingCount']").InnerText.Trim());
                    novel.Status = novelDetailNode.SelectNodes("./div[@class='col-left']/div[@class='metas']/div")[2].InnerText.Trim();

                    if (novel.Status == "Hoàn")
                    {
                        novel.Status = "Full";
                    }

                    novel.CoverImage = novelDetailNode.SelectSingleNode("./div[@class='col-left']/div[@class='books']/div[@class='book']/noscript/img").GetAttributeValue("src", "").Trim();

                    List<Genre> genres = new List<Genre>();
                    HtmlNodeCollection genreNodes = novelDetailNode.SelectNodes("./div[@class='col-left']/div[@class='metas']/div/a[@itemprop='genre']");

                    foreach (var node in genreNodes)
                    {
                        Genre genre = new Genre()
                        {
                            Name = node.InnerText.Trim(),
                            Slug = node.GetAttributeValue("href", "").Split("/", StringSplitOptions.RemoveEmptyEntries)[2]
                        };

                        genres.Add(genre);
                    }

                    novel.Genres = genres;
                }
                else
                {
                    throw new RequestExeption(HttpStatusCode.NotFound, "Novel not found in crawled server");
                }
            }

            return novel;
        }
    }
}