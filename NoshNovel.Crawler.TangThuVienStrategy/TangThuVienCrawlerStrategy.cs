using HtmlAgilityPack;
using NoshNovel.Models;
using NoshNovel.Plugin.Strategies;
using NoshNovel.Plugin.Strategies.Attributes;
using NoshNovel.Plugin.Strategies.Exeptions;
using NoshNovel.Plugin.Strategies.Utilities;
using System.Net;

namespace NoshNovel.Server.TangThuVienStrategy
{
    [NovelServer("truyen.tangthuvien.vn")]
    public partial class TangThuVienCrawlerStrategy : INovelCrawlerStrategy
    {
        public async Task<NovelSearchResult> FilterByGenre(string genre, int page = 1, int perPage = 18)
        {
            // Calculate page and position to crawl
            int startPosition = (page - 1) * perPage + 1;
            int firstCrawledPage = startPosition / maxPerCrawlPage + (startPosition % maxPerCrawlPage == 0 ? 0 : 1);
            int crawlPosition = (startPosition - 1) % maxPerCrawlPage;

            genre = HelperClass.GenerateSlug(genre);
            var url = $"{baseUrl}/the-loai/{genre}";

            NovelSearchResult searchResult = new NovelSearchResult();
            searchResult.Page = page;
            searchResult.PerPage = perPage;

            using (HttpClient httpClient = new HttpClient())
            {
                // make first request
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
                HttpResponseMessage response = await httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    string responseContent = await response.Content.ReadAsStringAsync();
                    // Decodes html-encoded
                    responseContent = WebUtility.HtmlDecode(responseContent);
                    HtmlDocument doc = new HtmlDocument();
                    doc.LoadHtml(responseContent);

                    HtmlNode showMoreNode = doc.DocumentNode.SelectSingleNode("//div[@class='left-wrap fl']/h3/a");

                    if (showMoreNode != null)
                    {
                        // Obtain new url
                        string newUrl = showMoreNode.GetAttributeValue("href", "");
                        // Make second request
                        request = new HttpRequestMessage(HttpMethod.Get, newUrl);
                        response = await httpClient.SendAsync(request);

                        if (response.IsSuccessStatusCode)
                        {
                            responseContent = await response.Content.ReadAsStringAsync();
                            // Decodes html-encoded
                            responseContent = WebUtility.HtmlDecode(responseContent);
                            doc.LoadHtml(responseContent);

                            HtmlNodeCollection paginationElementNodes = doc.DocumentNode.SelectNodes("//ul[@class='pagination']/li/a");

                            if (paginationElementNodes != null)
                            {
                                // Total crawl pages
                                HtmlNode lastNumNode = paginationElementNodes[paginationElementNodes.Count - 2];
                                int totalCrawlPages = int.Parse(lastNumNode.InnerText);

                                int novelCountDown = perPage;
                                List<NovelItem> novelItems = new List<NovelItem>();
                                // Crawl novel items
                                for (int i = firstCrawledPage; i <= totalCrawlPages && novelCountDown > 0; i++)
                                {
                                    // If the first crawled page equals one, no need to make new request
                                    if (i > 1)
                                    {
                                        // Make more requests
                                        url = $"{newUrl}&page={i}";
                                        request = new HttpRequestMessage(HttpMethod.Get, url);
                                        response = await httpClient.SendAsync(request);

                                        if (response.IsSuccessStatusCode)
                                        {
                                            responseContent = response.Content.ReadAsStringAsync().Result;
                                            // Decodes html-encoded
                                            responseContent = WebUtility.HtmlDecode(responseContent);
                                            doc.LoadHtml(responseContent);
                                        }
                                        else
                                        {
                                            throw new RequestExeption(HttpStatusCode.NotFound, "No content found in crawled server");
                                        }
                                    }

                                    HtmlNodeCollection novelNodes = doc.DocumentNode.SelectNodes("//div[@class='main-content-wrap fl']/div[@class='rank-body']/div/div/ul/li");

                                    for (int j = crawlPosition; j < novelNodes.Count; j++)
                                    {
                                        NovelItem novelItem = new NovelItem();

                                        HtmlNode novelNode = novelNodes[j];

                                        novelItem.Title = novelNode.SelectSingleNode("./div[@class='book-mid-info']/h4/a").InnerText.Trim();


                                        // Author
                                        HtmlNode authorNode = novelNode.SelectSingleNode("./div[@class='book-mid-info']/p[@class='author']/a[@class='name']");
                                        if (authorNode != null)
                                        {
                                            string[] authorLinkTokens = authorNode.GetAttributeValue("href", "").Split('=', StringSplitOptions.RemoveEmptyEntries);
                                            novelItem.Author = novelItem.Author = new Author()
                                            {
                                                Name = authorNode.InnerText.Trim(),
                                                Slug = authorLinkTokens[authorLinkTokens.Length - 1]
                                            };

                                            // Genre
                                            HtmlNodeCollection infoLinkNodes = novelNode.SelectNodes("./div[@class='book-mid-info']/p[@class='author']/a");
                                            HtmlNode genreNode = infoLinkNodes[infoLinkNodes.Count - 1];
                                            if (genreNode != null && genreNode.Name == "a")
                                            {
                                                string[] genreLinkTokens = genreNode.GetAttributeValue("href", "").Split('/', StringSplitOptions.RemoveEmptyEntries);
                                                List<Genre> genres = new List<Genre>()
                                                {
                                                    new Genre()
                                                    {
                                                        Name = genreNode.InnerText.Trim(),
                                                        Slug = genreLinkTokens[genreLinkTokens.Length - 1]
                                                    }
                                                };
                                                novelItem.Genres = genres;
                                            }
                                        }

                                        novelItem.CoverImage = novelNode.SelectSingleNode("./div[@class='book-img-box']/a/img").GetAttributeValue("src", "").Trim();
                                        novelItem.NovelSlug = novelNode.SelectSingleNode("./div[@class='book-mid-info']/h4/a").GetAttributeValue("href", "").Split("/", StringSplitOptions.RemoveEmptyEntries)[3].Trim();
                                        novelItem.Status = novelNode.SelectSingleNode("./div[@class='book-mid-info']/p[@class='author']/span").InnerText.Trim();
                                        novelItem.TotalChapter = int.Parse(novelNode.SelectNodes("./div[@class='book-mid-info']/p[@class='author']/span")[1].SelectSingleNode("./span").InnerText);
                                        novelItem.Description = novelNode.SelectNodes("./div[@class='book-mid-info']/p")[1].InnerText.Trim();

                                        novelItems.Add(novelItem);

                                        if (--novelCountDown == 0)
                                        {
                                            break;
                                        }
                                    }

                                    crawlPosition = 0;
                                }

                                searchResult.Data = novelItems;

                                url = $"{newUrl}&page={totalCrawlPages}";
                                // Calculate total novels
                                request = new HttpRequestMessage(HttpMethod.Get, url);
                                response = httpClient.Send(request);

                                if (response.IsSuccessStatusCode)
                                {
                                    responseContent = await response.Content.ReadAsStringAsync();
                                    // Decodes html-encoded
                                    responseContent = WebUtility.HtmlDecode(responseContent);
                                    doc.LoadHtml(responseContent);

                                    int totalNovels = 0;

                                    HtmlNodeCollection lastPageNodes = doc.DocumentNode.SelectNodes("//div[@class='main-content-wrap fl']/div[@class='rank-body']/div/div/ul/li");

                                    if (lastPageNodes != null)
                                    {
                                        int novelOflastCrawedPage = lastPageNodes.Count;
                                        totalNovels = (totalCrawlPages - 1) * maxPerCrawlPage + novelOflastCrawedPage;
                                    }

                                    searchResult.Total = totalNovels;
                                    searchResult.TotalPages = totalNovels / perPage + (totalNovels % perPage == 0 ? 0 : 1);
                                }
                            }
                        }
                    }
                }
                else
                {
                    throw new RequestExeption(HttpStatusCode.NotFound, "Genre not found in crawled server");
                }
            }

            return searchResult;
        }

        public async Task<NovelSearchResult> FilterByAuthor(string author, int page = 1, int perPage = 18)
        {
            throw new NotImplementedException();
        }

        public async Task<NovelSearchResult> GetByKeyword(string keyword, int page = 1, int perPage = 18)
        {
            // Calculate page and position to crawl
            int startPosition = (page - 1) * perPage + 1;
            int firstCrawledPage = startPosition / maxPerCrawlPage + (startPosition % maxPerCrawlPage == 0 ? 0 : 1);
            int crawlPosition = (startPosition - 1) % maxPerCrawlPage;

            keyword = string.Join("%20", keyword.Split(" ", StringSplitOptions.RemoveEmptyEntries));
            var url = $"{baseUrl}/ket-qua-tim-kiem?term={keyword}";

            NovelSearchResult searchResult = new NovelSearchResult();
            searchResult.Page = page;
            searchResult.PerPage = perPage;

            using (HttpClient httpClient = new HttpClient())
            {
                // make first request
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
                HttpResponseMessage response = await httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    string responseContent = await response.Content.ReadAsStringAsync();
                    // Decodes html-encoded
                    responseContent = WebUtility.HtmlDecode(responseContent);
                    HtmlDocument doc = new HtmlDocument();
                    doc.LoadHtml(responseContent);

                    // Total crawl pages
                    int totalCrawlPages = 1;

                    HtmlNodeCollection paginationElementNodes = doc.DocumentNode.SelectNodes("//ul[@class='pagination']/li/a");

                    // In case more than one page returned
                    if (paginationElementNodes != null)
                    {
                        HtmlNode lastNumNode = paginationElementNodes[paginationElementNodes.Count - 2];
                        totalCrawlPages = int.Parse(lastNumNode.InnerText);
                    }

                    int novelCountDown = perPage;
                    List<NovelItem> novelItems = new List<NovelItem>();
                    // Crawl novel items
                    for (int i = firstCrawledPage; i <= totalCrawlPages && novelCountDown > 0; i++)
                    {
                        // If the first crawled page equals one, no need to make new request
                        if (i > 1)
                        {
                            // Make more requests
                            url = $"{baseUrl}/ket-qua-tim-kiem?term={keyword}&page={i}";
                            request = new HttpRequestMessage(HttpMethod.Get, url);
                            response = await httpClient.SendAsync(request);

                            if (response.IsSuccessStatusCode)
                            {
                                responseContent = await response.Content.ReadAsStringAsync();
                                // Decodes html-encoded
                                responseContent = WebUtility.HtmlDecode(responseContent);
                                doc.LoadHtml(responseContent);
                            }
                        }

                        HtmlNodeCollection novelNodes = doc.DocumentNode.SelectNodes("//div[@class='main-content-wrap fl']/div[@class='rank-body']/div/div/ul/li");

                        // Not found novels
                        if (novelNodes == null || novelNodes[0].SelectSingleNode("./div") == null)
                        {
                            break;
                        }

                        for (int j = crawlPosition; j < novelNodes.Count; j++)
                        {
                            NovelItem novelItem = new NovelItem();

                            HtmlNode novelNode = novelNodes[j];

                            novelItem.Title = novelNode.SelectSingleNode("./div[@class='book-mid-info']/h4/a").InnerText.Trim();

                            // Author
                            HtmlNode authorNode = novelNode.SelectSingleNode("./div[@class='book-mid-info']/p[@class='author']/a[@class='name']");
                            if (authorNode != null)
                            {
                                string[] authorLinkTokens = authorNode.GetAttributeValue("href", "").Split('=', StringSplitOptions.RemoveEmptyEntries);
                                novelItem.Author = novelItem.Author = new Author()
                                {
                                    Name = authorNode.InnerText.Trim(),
                                    Slug = authorLinkTokens[authorLinkTokens.Length - 1]
                                };

                                // Genre
                                HtmlNodeCollection infoLinkNodes = novelNode.SelectNodes("./div[@class='book-mid-info']/p[@class='author']/a");
                                HtmlNode genreNode = infoLinkNodes[infoLinkNodes.Count - 1];
                                if (genreNode != null && genreNode.Name == "a")
                                {
                                    string[] genreLinkTokens = genreNode.GetAttributeValue("href", "").Split('/', StringSplitOptions.RemoveEmptyEntries);
                                    List<Genre> genres = new List<Genre>()
                                    {
                                        new Genre()
                                        {
                                            Name = genreNode.InnerText.Trim(),
                                            Slug = genreLinkTokens[genreLinkTokens.Length - 1]
                                        }
                                    };
                                    novelItem.Genres = genres;
                                }
                            }

                            novelItem.CoverImage = novelNode.SelectSingleNode("./div[@class='book-img-box']/a/img").GetAttributeValue("src", "").Trim();
                            // Get novel slug
                            HtmlNode novelLinkNode = novelNode.SelectSingleNode("./div[@class='book-mid-info']/h4/a");
                            var novelLinkHtmlParts = novelLinkNode.OuterHtml.Split(" ", StringSplitOptions.RemoveEmptyEntries);

                            if (novelLinkHtmlParts[1][^2] == '/')
                            {
                                string splitedString = novelLinkHtmlParts[2].Split("=")[0];
                                splitedString = splitedString.Remove(splitedString.Length - 1);
                                novelItem.NovelSlug = $"\"{splitedString}";
                            }
                            else
                            {
                                novelItem.NovelSlug = novelLinkNode.GetAttributeValue("href", "").Split("/", StringSplitOptions.RemoveEmptyEntries)[3].Trim();
                            }

                            novelItem.Status = novelNode.SelectSingleNode("./div[@class='book-mid-info']/p[@class='author']/span").InnerText.Trim();
                            novelItem.TotalChapter = int.Parse(novelNode.SelectNodes("./div[@class='book-mid-info']/p[@class='author']/span")[1].SelectSingleNode("./span").InnerText);
                            novelItem.Description = novelNode.SelectNodes("./div[@class='book-mid-info']/p")[1].InnerText.Trim();

                            novelItems.Add(novelItem);

                            if (--novelCountDown == 0)
                            {
                                break;
                            }
                        }

                        crawlPosition = 0;
                    }

                    searchResult.Data = novelItems;

                    url = $"{baseUrl}/ket-qua-tim-kiem?term={keyword}&page={totalCrawlPages}";
                    // Calculate total novels
                    request = new HttpRequestMessage(HttpMethod.Get, url);
                    response = httpClient.Send(request);

                    if (response.IsSuccessStatusCode)
                    {
                        responseContent = await response.Content.ReadAsStringAsync();
                        // Decodes html-encoded
                        responseContent = WebUtility.HtmlDecode(responseContent);
                        doc.LoadHtml(responseContent);

                        int totalNovels = 0;

                        HtmlNodeCollection lastPageNodes = doc.DocumentNode.SelectNodes("//div[@class='main-content-wrap fl']/div[@class='rank-body']/div/div/ul/li");

                        if (lastPageNodes != null && lastPageNodes[0].SelectSingleNode("./div") != null)
                        {
                            int novelOflastCrawedPage = lastPageNodes.Count;
                            totalNovels = (totalCrawlPages - 1) * maxPerCrawlPage + novelOflastCrawedPage;
                        }

                        searchResult.Total = totalNovels;
                        searchResult.TotalPages = totalNovels / perPage + (totalNovels % perPage == 0 ? 0 : 1);
                    }
                    else
                    {
                        throw new RequestExeption(HttpStatusCode.NotFound, "No content found in crawled server!");
                    }
                }
                else
                {
                    throw new RequestExeption(HttpStatusCode.NotFound, "No content found in crawled server!");
                }
            }

            return searchResult;
        }

        public async Task<NovelChaptersResult> GetChapterList(string novelSlug, int page = 1, int perPage = 40)
        {
            var url = $"{baseUrl}/doc-truyen/{novelSlug}";
            int startPosition = (page - 1) * perPage + 1;

            NovelChaptersResult chaptersResult = new NovelChaptersResult();
            chaptersResult.Page = page;
            chaptersResult.PerPage = perPage;

            using (HttpClient httpClient = new HttpClient())
            {
                // make first request
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
                HttpResponseMessage response = await httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    string responseContent = await response.Content.ReadAsStringAsync();
                    // Decodes html-encoded
                    responseContent = WebUtility.HtmlDecode(responseContent);
                    HtmlDocument doc = new HtmlDocument();
                    doc.LoadHtml(responseContent);

                    string novelId = doc.DocumentNode.SelectSingleNode("//input[@id='story_id_hidden']").GetAttributeValue("value", "").Trim();

                    HtmlNodeCollection paginationElementNodes = doc.DocumentNode.SelectNodes("//ul[@class='pagination']/li/a");

                    // Get total chapters and total pages
                    if (paginationElementNodes != null)
                    {
                        int totalCrawlPages = 1;
                        // Total crawl pages
                        if (paginationElementNodes.Count > 1)
                        {
                            var lastPaginationNodeContent = paginationElementNodes[^1].SelectSingleNode("./span").InnerText.Trim();

                            if (lastPaginationNodeContent == "»")
                            {
                                totalCrawlPages = int.Parse(paginationElementNodes[^2].InnerText.Trim());
                            }

                            else if (lastPaginationNodeContent == "Trang cuối")
                            {
                                totalCrawlPages = int.Parse(paginationElementNodes[^1].GetAttributeValue("onclick", "").Split('(', ')')[1]) + 1;
                            }
                        }

                        url = $"{baseUrl}/doc-truyen/page/{novelId}?page={totalCrawlPages - 1}&limit={maxPerCrawledChaptersPage}&web=1";

                        request = new HttpRequestMessage(HttpMethod.Get, url);
                        response = httpClient.Send(request);

                        if (response.IsSuccessStatusCode)
                        {
                            responseContent = await response.Content.ReadAsStringAsync();
                            // Decodes html-encoded
                            responseContent = WebUtility.HtmlDecode(responseContent);
                            doc = new HtmlDocument();
                            doc.LoadHtml(responseContent);

                            int totalChapters = 0;

                            HtmlNodeCollection lastCrawedPageChapterNodes = doc.DocumentNode.SelectNodes("//ul[@class='cf']/li[not(contains(@class, 'divider-chap'))]");

                            if (lastCrawedPageChapterNodes != null)
                            {
                                int novelOflastCrawedPage = doc.DocumentNode.SelectNodes("//ul[@class='cf']/li[not(contains(@class, 'divider-chap'))]").Count;
                                totalChapters = (totalCrawlPages - 1) * maxPerCrawledChaptersPage + novelOflastCrawedPage;
                            }

                            chaptersResult.Total = totalChapters;
                            chaptersResult.TotalPages = totalChapters / perPage + (totalChapters % perPage == 0 ? 0 : 1);
                        }
                        else
                        {
                            throw new RequestExeption(HttpStatusCode.NotFound, "No content found in crawled server");
                        }
                    }


                    url = $"{baseUrl}/doc-truyen/page/{novelId}?page={page - 1}&limit={perPage}&web=1";

                    request = new HttpRequestMessage(HttpMethod.Get, url);
                    response = await httpClient.SendAsync(request);
                    // get chapters
                    if (response.IsSuccessStatusCode)
                    {
                        responseContent = await response.Content.ReadAsStringAsync();
                        // Decodes html-encoded
                        responseContent = WebUtility.HtmlDecode(responseContent);
                        doc = new HtmlDocument();
                        doc.LoadHtml(responseContent);

                        List<Chapter> chapterList = new List<Chapter>();

                        HtmlNodeCollection chapterNodes = doc.DocumentNode.SelectNodes("//ul[@class='cf']/li[not(contains(@class, 'divider-chap'))]/a");

                        if (chapterNodes != null)
                        {
                            foreach (var chapterNode in chapterNodes)
                            {
                                string chapterLabel = string.Empty;
                                string chapterName = string.Empty;
                                string chapterSlug = string.Empty;

                                if (chapterNode != null)
                                {
                                    var chapterTitleParts = chapterNode.GetAttributeValue("title", "").Split(":");

                                    chapterLabel = chapterTitleParts[0].Trim();

                                    if (chapterTitleParts.Length > 1)
                                    {
                                        chapterName = string.Join(" : ", chapterTitleParts.Skip(1).Select(part => part.Trim()));
                                    }

                                    chapterSlug = chapterNode.GetAttributeValue("href", "").Split("/", StringSplitOptions.RemoveEmptyEntries)[4];
                                }

                                Chapter chapter = new Chapter()
                                {
                                    Label = chapterLabel,
                                    Name = chapterName,
                                    Slug = chapterSlug,
                                    ChapterIndex = startPosition++
                                };

                                chapterList.Add(chapter);
                            }
                        }

                        chaptersResult.Data = chapterList;
                    }
                    else
                    {
                        throw new RequestExeption(HttpStatusCode.NotFound, "No content found in crawled server");
                    }
                }
                else
                {
                    throw new RequestExeption(HttpStatusCode.NotFound, "Novel not found in crawled server");
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
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
                HttpResponseMessage response = await httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    string responseContent = await response.Content.ReadAsStringAsync();
                    // decodes html-encoded
                    responseContent = WebUtility.HtmlDecode(responseContent);
                    HtmlDocument doc = new HtmlDocument();
                    doc.LoadHtml(responseContent);
                    HtmlNodeCollection genreNodes = doc.DocumentNode.SelectNodes("//div[@id='classify-list']/dl/dd");

                    if (genreNodes != null)
                    {
                        genreNodes.RemoveAt(genreNodes.Count - 1);

                        foreach (var genreNode in genreNodes)
                        {
                            HtmlNode genreLinkNode = genreNode.SelectSingleNode("a");
                            HtmlNode genreNameNode = genreNode.SelectSingleNode("a/cite/span/i");
                            if (genreLinkNode != null && genreNameNode != null)
                            {
                                string genreName = genreNameNode.InnerText.Trim();
                                List<string> linkParts = new List<string>(genreLinkNode.GetAttributeValue("href", "").Split("/", StringSplitOptions.RemoveEmptyEntries));

                                if (linkParts.Count == 2)
                                {
                                    continue;
                                }

                                string slug = linkParts[3];

                                Genre genre = new Genre()
                                {
                                    Name = genreNameNode.InnerText.Trim(),
                                    Slug = slug
                                };
                                genres.Add(genre);
                            }
                        }
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
            var url = $"{baseUrl}/doc-truyen/{novelSlug}/{chapterSlug}";

            NovelContent novelContentResult = new NovelContent();

            using (HttpClient httpClient = new HttpClient())
            {
                // make first request
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
                HttpResponseMessage response = await httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    string responseContent = await response.Content.ReadAsStringAsync();
                    // Decodes html-encoded
                    responseContent = WebUtility.HtmlDecode(responseContent);
                    HtmlDocument doc = new HtmlDocument();
                    doc.LoadHtml(responseContent);

                    HtmlNode titleNode = doc.DocumentNode.SelectSingleNode("//h1[@class='truyen-title']/a");

                    if (titleNode != null)
                    {
                        novelContentResult.Title = titleNode.InnerText.Trim();
                    }
                    else
                    {
                        throw new RequestExeption(HttpStatusCode.NotFound, "Chapter found in crawled server");
                    }

                    HtmlNode contentNode = doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'box-chap') and not(contains(@class, 'hidden'))]");

                    if (contentNode != null)
                    {
                        novelContentResult.Content = contentNode.InnerHtml;
                    }
                    else
                    {
                        throw new RequestExeption(HttpStatusCode.NotFound, "Chapter found in crawled server");
                    }

                    novelContentResult.Chapter = new Chapter();

                    HtmlNode chapterNode = doc.DocumentNode.SelectSingleNode("//div[@class='content']/div[@class='col-xs-12 chapter']/h2");

                    if (chapterNode != null)
                    {
                        novelContentResult.Chapter.Slug = chapterSlug;

                        var chapterParts = chapterNode.InnerText.Trim().Split(":");
                        novelContentResult.Chapter.Label = chapterParts[0].Trim();

                        if (chapterParts.Length > 1)
                        {
                            novelContentResult.Chapter.Name = String.Join(" : ", chapterParts.Skip(1).Select(part => part.Trim()));
                        }
                    }
                }
                else
                {
                    throw new RequestExeption(HttpStatusCode.NotFound, "Novel not found in crawled server");
                }
            }

            return novelContentResult;
        }

        public async Task<NovelDetail> GetNovelDetail(string novelSlug)
        {
            var url = $"{baseUrl}/doc-truyen/{novelSlug}";

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

                    HtmlNode novelTitleNode = doc.DocumentNode.SelectSingleNode("//div[@class='book-info ']/h1");

                    if (novelTitleNode != null)
                    {
                        novel.Title = novelTitleNode.InnerText.Trim();
                    }

                    HtmlNode novelAuthorNode = doc.DocumentNode.SelectSingleNode("//div[@class='book-info ']/p[@class='tag']/a[@class='blue']");

                    if (novelAuthorNode != null)
                    {
                        string[] authorLinkTokens = novelAuthorNode.GetAttributeValue("href", "").Split('=');
                        novel.Author = new Author()
                        {
                            Name = novelAuthorNode.InnerText.Trim(),
                            Slug = authorLinkTokens[authorLinkTokens.Length - 1]
                        };
                    }

                    HtmlNode novelDescriptionNode = doc.DocumentNode.SelectSingleNode("//div[@class='book-intro']/p");

                    if (novelDescriptionNode != null)
                    {
                        novel.Description = novelDescriptionNode.InnerHtml.Trim();
                    }

                    HtmlNode novelRatingNode = doc.DocumentNode.SelectSingleNode("//cite[@id='myrate']");

                    if (novelRatingNode != null)
                    {
                        novel.Rating = double.Parse(novelRatingNode.InnerText.Trim());
                    }

                    // In subdomain ngontinh
                    novelRatingNode = doc.DocumentNode.SelectSingleNode("//cite[@id='score1']");

                    if (novelRatingNode != null)
                    {
                        novel.Rating = double.Parse(novelRatingNode.InnerText.Trim());
                    }

                    HtmlNode reviewsNumberNode = doc.DocumentNode.SelectSingleNode("//p[@id='j_userCount']").SelectSingleNode(".//span");

                    if (reviewsNumberNode != null)
                    {
                        novel.ReviewsNumber = int.Parse(reviewsNumberNode.InnerText.Trim());
                    }

                    HtmlNode novelStatusNode = doc.DocumentNode.SelectSingleNode("//div[@class='book-info ']/p[@class='tag']/span");

                    if (novelStatusNode != null)
                    {
                        novel.Status = novelStatusNode.InnerText.Trim();
                    }

                    HtmlNode novelCoverImageNode = doc.DocumentNode.SelectSingleNode("//div[@class='book-img']/a/img");

                    if (novelCoverImageNode != null)
                    {
                        novel.CoverImage = novelCoverImageNode.GetAttributeValue("src", "").Trim();
                    }

                    HtmlNode novelGenresNode = doc.DocumentNode.SelectSingleNode("//div[@class='book-info ']/p[@class='tag']/a[@class='red']");

                    if (novelGenresNode != null)
                    {
                        List<Genre> genreList = new List<Genre>();
                        Genre genre = new Genre()
                        {
                            Name = novelGenresNode.InnerText.Trim(),
                            Slug = novelGenresNode.GetAttributeValue("href", "").Split("/", StringSplitOptions.RemoveEmptyEntries)[3]
                        };
                        genreList.Add(genre);

                        novel.Genres = genreList;
                    }
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


