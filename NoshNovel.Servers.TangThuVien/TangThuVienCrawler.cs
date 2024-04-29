using System.Net.Mime;
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
            throw new NotImplementedException();
        }

        public NovelSearchResult GetByKeyword(string keyword, int page = 1, int perPage = 18)
        {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }
    }
}


