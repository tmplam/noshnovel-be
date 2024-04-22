using NoshNovel.Models;

namespace NoshNovel.Plugins
{
    public interface INovelCrawler
    {
        IEnumerable<NovelItem> GetNovels(string keyword);
        IEnumerable<Genre> GetGenres();
        NovelDetail GetNovelDetail(string novelUrl);
    }
}
