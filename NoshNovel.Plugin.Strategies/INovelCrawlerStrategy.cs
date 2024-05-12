using NoshNovel.Models;

namespace NoshNovel.Plugin.Strategies
{
    public interface INovelCrawlerStrategy
    {
        Task<NovelSearchResult> GetByKeyword(string keyword, int page = 1, int perPage = 18);
        Task<NovelSearchResult> FilterByGenre(string genre, int page = 1, int perPage = 18);
        // Get genre list of novel server
        Task<IEnumerable<Genre>> GetGenres();
        Task<NovelDetail> GetNovelDetail(string novelSlug);
        Task<NovelChaptersResult> GetChapterList(string novelSlug, int page = 1, int perPage = 40);
        Task<NovelContent> GetNovelContent(string novelSlug, string chapterSlug);
    }
}
