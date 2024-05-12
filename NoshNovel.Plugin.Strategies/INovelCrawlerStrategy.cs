using NoshNovel.Models;

namespace NoshNovel.Plugin.Strategies
{
    public interface INovelCrawlerStrategy
    {
        NovelSearchResult GetByKeyword(string keyword, int page = 1, int perPage = 18);
        NovelSearchResult FilterByGenre(string genre, int page = 1, int perPage = 18);
        // Get genre list of novel server
        IEnumerable<Genre> GetGenres();
        NovelDetail GetNovelDetail(string novelSlug);
        NovelChaptersResult GetChapterList(string novelSlug, int page = 1, int perPage = 40);
        NovelContent GetNovelContent(string novelSlug, string chapterSlug);
    }
}
