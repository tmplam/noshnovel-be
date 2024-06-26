﻿using HtmlAgilityPack;
using NoshNovel.Models;
using NoshNovel.Plugin.Strategies;
using NoshNovel.Plugin.Strategies.Attributes;
using NoshNovel.Plugin.Strategies.Exeptions;
using NoshNovel.Plugin.Strategies.Utilities;
using System.Net;
using System.Text.RegularExpressions;

namespace NoshNovel.Server.TruyenFullStrategy
{
    [NovelServer("truyenfull.vn")]
    public partial class TruyenFullCrawlerStrategy : INovelCrawlerStrategy
    {
        public async Task<NovelSearchResult> GetByKeyword(string keyword, int page = 1, int perPage = 18)
        {
            // Calculate page and position to crawl
            int startPosition = (page - 1) * perPage + 1;
            int firstCrawledPage = startPosition / maxPerCrawlPage + (startPosition % maxPerCrawlPage == 0 ? 0 : 1);
            int crawlPosition = (startPosition - 1) % maxPerCrawlPage;


            keyword = string.Join("%20", keyword.Split(" ", StringSplitOptions.RemoveEmptyEntries));
            string url = $"{baseUrl}/tim-kiem/?tukhoa={keyword}";

            using HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders
                .Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.110 Safari/537.3");
            HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
            HttpResponseMessage responseMessage = await httpClient.SendAsync(requestMessage);

            if (!responseMessage.IsSuccessStatusCode)
            {
                throw new RequestExeption(HttpStatusCode.NotFound, "No content found in crawled server");
            }

            string htmlContent = await responseMessage.Content.ReadAsStringAsync();
            htmlContent = WebUtility.HtmlDecode(htmlContent);

            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(htmlContent);

            // Calculate total crawled pages
            HtmlNode paginationNode = doc.DocumentNode.SelectSingleNode(".//ul[contains(@class, 'pagination')]");

            int totalCrawlPages = 1;

            if (paginationNode != null)
            {
                HtmlNode lastNode = paginationNode.Descendants("li")!.LastOrDefault()!.SelectSingleNode(".//a");
                HtmlNode penultimateNode = paginationNode.Descendants("li")!.LastOrDefault()!.PreviousSibling.SelectSingleNode(".//a");

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
                requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
                responseMessage = await httpClient.SendAsync(requestMessage);

                if (!responseMessage.IsSuccessStatusCode)
                {
                    throw new RequestExeption(HttpStatusCode.NotFound, "No content found in crawled server");
                }

                htmlContent = await responseMessage.Content.ReadAsStringAsync();
                htmlContent = WebUtility.HtmlDecode(htmlContent);
                doc.LoadHtml(htmlContent);

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
                            string[] chapterStringTokens = novelNode.SelectSingleNode(".//span[@class='chapter-text']").ParentNode.InnerText
                                .Split(" ", StringSplitOptions.RemoveEmptyEntries);

                            string chapterNumString = chapterStringTokens[chapterStringTokens.Length - 1];
                            if (chapterNumString.Contains("-"))
                            {
                                chapterNumString = chapterNumString.Split("-")[0];
                            }
                            novelItem.TotalChapter = int.Parse(chapterNumString);

                        }
                        catch (Exception)
                        {
                            novelItem.TotalChapter = 0;
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
            requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
            responseMessage = await httpClient.SendAsync(requestMessage);

            if (!responseMessage.IsSuccessStatusCode)
            {
                throw new RequestExeption(HttpStatusCode.NotFound, "No content found in crawled server");
            }

            htmlContent = await responseMessage.Content.ReadAsStringAsync();
            htmlContent = WebUtility.HtmlDecode(htmlContent);

            doc.LoadHtml(htmlContent);
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

        public async Task<NovelSearchResult> FilterByGenre(string genre, int page = 1, int perPage = 18)
        {
            // Calculate page and position to crawl
            int startPosition = (page - 1) * perPage + 1;
            int firstCrawledPage = startPosition / maxPerCrawlPage + (startPosition % maxPerCrawlPage == 0 ? 0 : 1);
            int crawlPosition = (startPosition - 1) % maxPerCrawlPage;

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
            string url = $"{baseUrl}/the-loai/{genre}/";
            using HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders
                .Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.110 Safari/537.3");

            HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
            HttpResponseMessage responseMessage = await httpClient.SendAsync(requestMessage);

            if (!responseMessage.IsSuccessStatusCode)
            {
                throw new RequestExeption(HttpStatusCode.NotFound, "Genre not found in crawled server");
            }

            string htmlContent = await responseMessage.Content.ReadAsStringAsync();
            htmlContent = WebUtility.HtmlDecode(htmlContent);

            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(htmlContent);

            HtmlNode paginationNode = doc.DocumentNode.SelectSingleNode(".//ul[contains(@class, 'pagination')]");

            int totalCrawlPages = 1;

            if (paginationNode != null)
            {
                HtmlNode lastNode = paginationNode.Descendants("li")!.LastOrDefault()!.SelectSingleNode(".//a");
                HtmlNode penultimateNode = paginationNode.Descendants("li")!.LastOrDefault()!.PreviousSibling.SelectSingleNode(".//a");

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
                requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
                responseMessage = await httpClient.SendAsync(requestMessage);

                if (!responseMessage.IsSuccessStatusCode)
                {
                    throw new RequestExeption(HttpStatusCode.NotFound, "No content found in crawled server");
                }

                htmlContent = await responseMessage.Content.ReadAsStringAsync();
                htmlContent = WebUtility.HtmlDecode(htmlContent);
                doc.LoadHtml(htmlContent);

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
                            novelItem.TotalChapter = 0;
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
            url = $"{baseUrl}/the-loai/{genre}/trang-{totalCrawlPages}";
            requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
            responseMessage = await httpClient.SendAsync(requestMessage);

            if (!responseMessage.IsSuccessStatusCode)
            {
                throw new RequestExeption(HttpStatusCode.NotFound, "No content found in crawled server");
            }

            htmlContent = await responseMessage.Content.ReadAsStringAsync();
            htmlContent = WebUtility.HtmlDecode(htmlContent);
            doc.LoadHtml(htmlContent);
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

        public async Task<NovelSearchResult> FilterByAuthor(string author, int page = 1, int perPage = 18)
        {
            // Calculate page and position to crawl
            int startPosition = (page - 1) * perPage + 1;
            int firstCrawledPage = startPosition / maxPerCrawlPage + (startPosition % maxPerCrawlPage == 0 ? 0 : 1);
            int crawlPosition = (startPosition - 1) % maxPerCrawlPage;

            author = HelperClass.GenerateSlug(author);

            // Calculate total crawled pages
            string url = $"{baseUrl}/tac-gia/{author}/";
            using HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders
                .Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.110 Safari/537.3");

            HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
            HttpResponseMessage responseMessage = await httpClient.SendAsync(requestMessage);

            if (!responseMessage.IsSuccessStatusCode)
            {
                throw new RequestExeption(HttpStatusCode.NotFound, "Author not found in crawled server");
            }

            string htmlContent = await responseMessage.Content.ReadAsStringAsync();
            htmlContent = WebUtility.HtmlDecode(htmlContent);

            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(htmlContent);

            HtmlNode paginationNode = doc.DocumentNode.SelectSingleNode(".//ul[contains(@class, 'pagination')]");

            int totalCrawlPages = 1;

            if (paginationNode != null)
            {
                HtmlNode lastNode = paginationNode.Descendants("li")!.LastOrDefault()!.SelectSingleNode(".//a");
                HtmlNode penultimateNode = paginationNode.Descendants("li")!.LastOrDefault()!.PreviousSibling.SelectSingleNode(".//a");

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
                url = $"{baseUrl}/tac-gia/{author}/trang-{i}";
                requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
                responseMessage = await httpClient.SendAsync(requestMessage);

                if (!responseMessage.IsSuccessStatusCode)
                {
                    throw new RequestExeption(HttpStatusCode.NotFound, "No content found in crawled server");
                }

                htmlContent = await responseMessage.Content.ReadAsStringAsync();
                htmlContent = WebUtility.HtmlDecode(htmlContent);
                doc.LoadHtml(htmlContent);

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
                            novelItem.TotalChapter = 0;
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
            url = $"{baseUrl}/tac-gia/{author}/trang-{totalCrawlPages}";
            requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
            responseMessage = await httpClient.SendAsync(requestMessage);

            if (!responseMessage.IsSuccessStatusCode)
            {
                throw new RequestExeption(HttpStatusCode.NotFound, "No content found in crawled server");
            }

            htmlContent = await responseMessage.Content.ReadAsStringAsync();
            htmlContent = WebUtility.HtmlDecode(htmlContent);
            doc.LoadHtml(htmlContent);
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

        public async Task<IEnumerable<Genre>> GetGenres()
        {
            var url = baseUrl;
            using HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders
                .Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.110 Safari/537.3");
            HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
            HttpResponseMessage responseMessage = await httpClient.SendAsync(requestMessage);

            if (!responseMessage.IsSuccessStatusCode)
            {
                throw new RequestExeption(HttpStatusCode.NotFound, "No content found in crawled server");
            }

            string htmlContent = await responseMessage.Content.ReadAsStringAsync();
            htmlContent = WebUtility.HtmlDecode(htmlContent);

            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(htmlContent);

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

        public async Task<NovelDetail> GetNovelDetail(string novelSlug)
        {
            NovelDetail novel = new NovelDetail();

            var url = $"{baseUrl}/{novelSlug}";
            using HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders
                .Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.110 Safari/537.3");
            HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
            HttpResponseMessage responseMessage = await httpClient.SendAsync(requestMessage);

            if (!responseMessage.IsSuccessStatusCode)
            {
                throw new RequestExeption(HttpStatusCode.NotFound, "Novel not found in crawled server");
            }

            string htmlContent = await responseMessage.Content.ReadAsStringAsync();
            htmlContent = WebUtility.HtmlDecode(htmlContent);

            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(htmlContent);

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

            HtmlNodeCollection infoCollection = infoNode.SelectNodes("div");
            novel.Status = infoCollection[infoCollection.Count() - 1].SelectSingleNode("span").InnerText.Trim();
            novel.Rating = double.Parse(doc.DocumentNode.SelectSingleNode("//span[@itemprop='ratingValue']").InnerText) / 2;
            novel.ReviewsNumber = int.Parse(doc.DocumentNode.SelectSingleNode("//span[@itemprop='ratingCount']").InnerText);
            novel.Description = doc.DocumentNode.SelectSingleNode("//div[@itemprop='description']").InnerHtml;

            return novel;
        }

        public async Task<NovelChaptersResult> GetChapterList(string novelSlug, int page = 1, int perPage = 40)
        {
            NovelChaptersResult response = new NovelChaptersResult();
            response.Page = page;
            response.PerPage = perPage;

            // Calculate page and position to crawl
            int startPosition = (page - 1) * perPage + 1;
            int firstCrawledPage = startPosition / maxPerCrawledChaptersPage + (startPosition % maxPerCrawledChaptersPage == 0 ? 0 : 1);
            int crawlPosition = (startPosition - 1) % maxPerCrawledChaptersPage;

            var novelUrl = $"{baseUrl}/{novelSlug}";

            using HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders
                .Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.110 Safari/537.3");
            HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, novelUrl);
            HttpResponseMessage responseMessage = await httpClient.SendAsync(requestMessage);

            if (!responseMessage.IsSuccessStatusCode)
            {
                throw new RequestExeption(HttpStatusCode.NotFound, "Novel not found in crawled server");
            }

            string htmlContent = await responseMessage.Content.ReadAsStringAsync();
            htmlContent = WebUtility.HtmlDecode(htmlContent);

            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(htmlContent);

            int totalCrawlPages = int.Parse(doc.DocumentNode.SelectSingleNode("//input[@id='total-page']").GetAttributeValue("value", "1"));

            int chapterCountDown = perPage;

            List<Chapter> chapters = new List<Chapter>();
            for (int i = firstCrawledPage; i <= totalCrawlPages && chapterCountDown > 0; i++)
            {
                string pageUrl = $"{novelUrl}/trang-{i}";
                requestMessage = new HttpRequestMessage(HttpMethod.Get, pageUrl);
                responseMessage = await httpClient.SendAsync(requestMessage);

                if (!responseMessage.IsSuccessStatusCode)
                {
                    throw new RequestExeption(HttpStatusCode.NotFound, "No content found in crawled server");
                }

                htmlContent = await responseMessage.Content.ReadAsStringAsync();
                htmlContent = WebUtility.HtmlDecode(htmlContent);

                doc.LoadHtml(htmlContent);

                HtmlNodeCollection chapterWrappers = doc.DocumentNode.SelectNodes("//ul[@class='list-chapter']");

                if (chapterWrappers != null)
                {
                    var chapterList = chapterWrappers.SelectMany(chapterWrapper => chapterWrapper.Descendants("a")).ToList();
                    for (int j = crawlPosition; j < chapterList.Count(); j++)
                    {
                        HtmlNode chapterNode = chapterList[j];
                        string[] chapterTokens = chapterNode.InnerText.Split(":", 2, StringSplitOptions.RemoveEmptyEntries);

                        Chapter chapter = new Chapter()
                        {
                            ChapterIndex = startPosition++
                        };
                        chapter.Label = chapterTokens[0].Trim();
                        chapter.Slug = chapterNode.GetAttributeValue("href", "").Split("/", StringSplitOptions.RemoveEmptyEntries)[3];

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

            // Calculate total items
            string lastPageUrl = $"{novelUrl}/trang-{totalCrawlPages}";
            requestMessage = new HttpRequestMessage(HttpMethod.Get, lastPageUrl);
            responseMessage = await httpClient.SendAsync(requestMessage);

            if (!responseMessage.IsSuccessStatusCode)
            {
                throw new RequestExeption(HttpStatusCode.NotFound, "No content found in crawled server");
            }

            htmlContent = await responseMessage.Content.ReadAsStringAsync();
            htmlContent = WebUtility.HtmlDecode(htmlContent);
            doc.LoadHtml(htmlContent);

            int totalChapters = 0;

            HtmlNodeCollection last = doc.DocumentNode.SelectNodes("//ul[@class='list-chapter']");
            if (last != null)
            {
                int novelOflastCrawedPage = last.SelectMany(chapterWrapper => chapterWrapper.Descendants("a")).Count();
                totalChapters = (totalCrawlPages - 1) * maxPerCrawledChaptersPage + novelOflastCrawedPage;
            };

            response.Total = totalChapters;
            response.TotalPages = totalChapters / perPage + (totalChapters % perPage == 0 ? 0 : 1);
            response.Data = chapters;

            return response;
        }

        public async Task<NovelContent> GetNovelContent(string novelSlug, string chapterSlug)
        {
            var novelContent = new NovelContent();

            var novelUrl = $"{baseUrl}/{novelSlug}/{chapterSlug}";
            using HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders
                .Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.110 Safari/537.3");
            HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, novelUrl);
            HttpResponseMessage responseMessage = await httpClient.SendAsync(requestMessage);

            if (!responseMessage.IsSuccessStatusCode)
            {
                throw new RequestExeption(HttpStatusCode.NotFound, "Novel not found in crawled server");
            }

            string htmlContent = await responseMessage.Content.ReadAsStringAsync();
            htmlContent = WebUtility.HtmlDecode(htmlContent);

            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(htmlContent);

            try
            {
                novelContent.Title = doc.DocumentNode.SelectSingleNode("//a[@class='truyen-title']").InnerText.Trim();

                HtmlNode firstDiv = doc.DocumentNode.SelectSingleNode("//div[@itemprop='articleBody']/div");
                if (firstDiv != null)
                {
                    HtmlNode contentNode = firstDiv.ParentNode;
                    contentNode.RemoveChild(firstDiv);

                    novelContent.Content = contentNode.InnerHtml;
                    novelContent.Content = Regex.Replace(novelContent.Content, "<script[^>]*>.*?</script>", "");
                }

                HtmlNode chapterNode = doc.DocumentNode.SelectSingleNode("//a[@class='chapter-title']");
                string[] chapterTokens = chapterNode.InnerText.Split(":", 2, StringSplitOptions.RemoveEmptyEntries);

                novelContent.Chapter = new Chapter();
                novelContent.Chapter.Label = chapterTokens[0].Trim();
                novelContent.Chapter.Slug = chapterNode.GetAttributeValue("href", "").Split("/", StringSplitOptions.RemoveEmptyEntries)[3];

                if (chapterTokens.Length > 1)
                {
                    novelContent.Chapter.Name = chapterTokens[1].Trim().Capitalize();
                }
            }
            catch (Exception)
            {
                throw new RequestExeption(HttpStatusCode.NotFound, "Chapter not found in crawled server");
            }

            return novelContent;
        }
    }
}
