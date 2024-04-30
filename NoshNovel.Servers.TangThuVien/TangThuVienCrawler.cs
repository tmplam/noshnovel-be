using System.Net.Mime;
using System.Runtime.CompilerServices;
using HtmlAgilityPack;
using NoshNovel.Models;
using NoshNovel.Plugins;
using NoshNovel.Plugins.Attributes;
using NoshNovel.Plugins.Utilities;

namespace NoshNovel.Servers.TangThuVien
{
    [NovelServer("tangthuvien.vn")]
    public partial class TangThuVienCrawler : INovelCrawler
    {
        public NovelSearchResult FilterByGenre(string genre, int page = 1, int perPage = 18)
        {
            // Calculate page and position to crawl
            int startPosition = (page - 1) * perPage + 1;
            int firstCrawledPage = startPosition / maxPerCrawlPage + 1;
            int crawlPosition = startPosition % maxPerCrawlPage - 1;

            genre = HelperClass.GenerateSlug(genre);
            var url = $"{baseUrl}/the-loai/{genre}";

            NovelSearchResult searchResult = new NovelSearchResult();
            searchResult.Page = page;
            searchResult.PerPage = perPage;

            using (HttpClient httpClient = new HttpClient())
            {
                try
                {
                    // make first request
                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
                    HttpResponseMessage response = httpClient.Send(request);

                    if (response.IsSuccessStatusCode)
                    {
                        string responseContent = response.Content.ReadAsStringAsync().Result;
                        // Decodes html-encoded
                        responseContent = System.Net.WebUtility.HtmlDecode(responseContent);
                        HtmlDocument doc = new HtmlDocument();
                        doc.LoadHtml(responseContent);

                        HtmlNode showMoreNode = doc.DocumentNode.SelectSingleNode("//div[@class='left-wrap fl']/h3/a");

                        if (showMoreNode != null)
                        {
                            // Obtain new url
                            string newUrl = showMoreNode.GetAttributeValue("href", "");
                            // Make second request
                            request = new HttpRequestMessage(HttpMethod.Get, newUrl);
                            response = httpClient.Send(request);

                            if (response.IsSuccessStatusCode)
                            {
                                responseContent = response.Content.ReadAsStringAsync().Result;
                                // Decodes html-encoded
                                responseContent = System.Net.WebUtility.HtmlDecode(responseContent);
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
                                        if (firstCrawledPage > 1)
                                        {
                                            // Make more requests
                                            url = $"{newUrl}&page={i}";
                                            request = new HttpRequestMessage(HttpMethod.Get, url);
                                            response = httpClient.Send(request);

                                            if (response.IsSuccessStatusCode)
                                            {
                                                responseContent = response.Content.ReadAsStringAsync().Result;
                                                // Decodes html-encoded
                                                responseContent = System.Net.WebUtility.HtmlDecode(responseContent);
                                                doc.LoadHtml(responseContent);
                                            }
                                        }

                                        HtmlNodeCollection novelNodes = doc.DocumentNode.SelectNodes("//div[@class='main-content-wrap fl']/div[@class='rank-body']/div/div/ul/li");

                                        for (int j = crawlPosition; j < novelNodes.Count; j++)
                                        {
                                            NovelItem novelItem = new NovelItem();

                                            HtmlNode novelNode = novelNodes[j];

                                            novelItem.Title = novelNode.SelectSingleNode("./div[@class='book-mid-info']/h4/a").InnerText.Trim();
                                            novelItem.Author = novelItem.Author = new Author()
                                            {
                                                Name = novelNode.SelectSingleNode("./div[@class='book-mid-info']/p[@class='author']/a[@class='name']").InnerText.Trim(),
                                            };
                                            novelItem.CoverImage = novelNode.SelectSingleNode("./div[@class='book-img-box']/a/img").GetAttributeValue("src", "").Trim();
                                            novelItem.NovelSlug = novelNode.SelectSingleNode("./div[@class='book-mid-info']/h4/a").GetAttributeValue("href", "").Split("/", StringSplitOptions.RemoveEmptyEntries)[3].Trim();
                                            novelItem.Status = novelNode.SelectSingleNode("./div[@class='book-mid-info']/p[@class='author']/span").InnerText.Trim();
                                            novelItem.TotalChapter = int.Parse(novelNode.SelectNodes("./div[@class='book-mid-info']/p[@class='author']/span")[1].SelectSingleNode("./span").InnerText);

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
                                        responseContent = response.Content.ReadAsStringAsync().Result;
                                        // Decodes html-encoded
                                        responseContent = System.Net.WebUtility.HtmlDecode(responseContent);
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
                }
                catch (HttpRequestException ex)
                {
                    Console.WriteLine($"An error occurred: {ex.Message}");
                }
            }

            return searchResult;
        }

        public NovelSearchResult GetByKeyword(string keyword, int page = 1, int perPage = 18)
        {
            // Calculate page and position to crawl
            int startPosition = (page - 1) * perPage + 1;
            int firstCrawledPage = startPosition / maxPerCrawlPage + 1;
            int crawlPosition = startPosition % maxPerCrawlPage - 1;

            keyword = string.Join("%20", keyword.Split(" ", StringSplitOptions.RemoveEmptyEntries));
            var url = $"{baseUrl}/ket-qua-tim-kiem?term={keyword}";

            NovelSearchResult searchResult = new NovelSearchResult();
            searchResult.Page = page;
            searchResult.PerPage = perPage;

            using (HttpClient httpClient = new HttpClient())
            {
                try
                {
                    // make first request
                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
                    HttpResponseMessage response = httpClient.Send(request);

                    if (response.IsSuccessStatusCode)
                    {
                        string responseContent = response.Content.ReadAsStringAsync().Result;
                        // Decodes html-encoded
                        responseContent = System.Net.WebUtility.HtmlDecode(responseContent);
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
                            if (firstCrawledPage > 1)
                            {
                                // Make more requests
                                url = $"{baseUrl}/ket-qua-tim-kiem?term={keyword}&page={i}";
                                request = new HttpRequestMessage(HttpMethod.Get, url);
                                response = httpClient.Send(request);

                                if (response.IsSuccessStatusCode)
                                {
                                    responseContent = response.Content.ReadAsStringAsync().Result;
                                    // Decodes html-encoded
                                    responseContent = System.Net.WebUtility.HtmlDecode(responseContent);
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
                                novelItem.Author = novelItem.Author = new Author()
                                {
                                    Name = novelNode.SelectSingleNode("./div[@class='book-mid-info']/p[@class='author']/a[@class='name']").InnerText.Trim(),
                                };
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
                            responseContent = response.Content.ReadAsStringAsync().Result;
                            // Decodes html-encoded
                            responseContent = System.Net.WebUtility.HtmlDecode(responseContent);
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
                    }
                }
                catch (HttpRequestException ex)
                {
                    Console.WriteLine($"An error occurred: {ex.Message}");
                }
            }

            return searchResult;
        }

        public NovelChaptersResult GetChapterList(string novelSlug, int page = 1, int perPage = 40)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Genre> GetGenres()
        {
            var url = baseUrl;
            List<Genre> genres = new List<Genre>();

            using (HttpClient httpClient = new HttpClient())
            {
                try
                {
                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
                    HttpResponseMessage response = httpClient.Send(request);

                    if (response.IsSuccessStatusCode)
                    {
                        string responseContent = response.Content.ReadAsStringAsync().Result;
                        // decodes html-encoded
                        responseContent = System.Net.WebUtility.HtmlDecode(responseContent);
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
                                    string slug = linkParts.Count == 2 ? "ngon-tinh" : linkParts[3];
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
                }
                catch (HttpRequestException ex)
                {
                    Console.WriteLine($"An error occurred: {ex.Message}");
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
            var url = $"{baseUrl}/doc-truyen/{novelSlug}";

            NovelDetail novel = new NovelDetail();

            using (HttpClient httpClient = new HttpClient())
            {
                try
                {
                    // make request
                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
                    HttpResponseMessage response = httpClient.Send(request);

                    if (response.IsSuccessStatusCode)
                    {
                        string responseContent = response.Content.ReadAsStringAsync().Result;
                        // Decodes html-encoded
                        responseContent = System.Net.WebUtility.HtmlDecode(responseContent);
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
                            novel.Author = new Author()
                            {
                                Name = novelAuthorNode.InnerText.Trim(),
                            };
                        }

                        HtmlNode novelDescriptionNode = doc.DocumentNode.SelectSingleNode("//div[@class='book-intro']/p");

                        if (novelDescriptionNode != null)
                        {
                            novel.Description = novelDescriptionNode.InnerText.Trim();
                        }

                        HtmlNode novelRatingNode = doc.DocumentNode.SelectSingleNode("//cite[@id='myrate']");

                        if (novelRatingNode != null)
                        {
                            novel.Rating = double.Parse(novelRatingNode.InnerText.Trim());
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
                }
                catch (HttpRequestException ex)
                {
                    Console.WriteLine($"An error occurred: {ex.Message}");
                }
            }

            return novel;
        }
    }
}


