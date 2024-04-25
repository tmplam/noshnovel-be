using NoshNovel.Models;

namespace NoshNovel.Plugins
{
    public interface INovelCrawler
    {
        NovelSearchResult GetByKeyword(string keyword, int page = 1, int perPage = 18);
        NovelSearchResult FilterByGenre(string genre, int page = 1, int perPage = 18);
        IEnumerable<Genre> GetGenres();
        NovelDetail GetNovelDetail(string novelSlug);
    }
}
