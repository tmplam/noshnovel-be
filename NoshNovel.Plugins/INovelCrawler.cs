using NoshNovel.Models;

namespace NoshNovel.Plugins
{
    public interface INovelCrawler
    {
        NovelSearchResult GetNovels(string keyword, string? genre = null, int page = 1, int perPage = 18);
        IEnumerable<Genre> GetGenres();
        NovelDetail GetNovelDetail(string novelUrl);
    }
}
