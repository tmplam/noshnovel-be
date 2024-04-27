using NoshNovel.Models;
using NoshNovel.Plugins;

namespace NoshNovel.Factories.NovelCrawlers
{
    public partial class PluginNovelCrawler : INovelCrawler
    {
        public NovelSearchResult FilterByGenre(string genre, int page = 1, int perPage = 18)
        {
            LoadPlugins();
            NovelSearchResult novelSearchResult;
            if (novelCrawler != null)
            {
                novelSearchResult = novelCrawler.FilterByGenre(genre, page, perPage);
            } 
            else
            {
                novelSearchResult = new NovelSearchResult();
            }
            RemovePlugin();

            return novelSearchResult;
        }

        public NovelSearchResult GetByKeyword(string keyword, int page = 1, int perPage = 18)
        {
            LoadPlugins();
            NovelSearchResult novelSearchResult;
            if (novelCrawler != null)
            {
                novelSearchResult = novelCrawler.GetByKeyword(keyword, page, perPage);
            }
            else
            {
                novelSearchResult = new NovelSearchResult();
            }
            RemovePlugin();

            return novelSearchResult;
        }

        public NovelChaptersResult GetChapterList(string novelSlug, int page = 1, int perPage = 40)
        {
            LoadPlugins();
            NovelChaptersResult novelChaptersResult;
            if (novelCrawler != null)
            {
                novelChaptersResult = novelCrawler.GetChapterList(novelSlug, page, perPage);
            }
            else
            {
                novelChaptersResult = new NovelChaptersResult();
            }
            RemovePlugin();

            return novelChaptersResult;
        }

        public IEnumerable<Genre> GetGenres()
        {
            LoadPlugins();
            IEnumerable<Genre> genreList;
            if (novelCrawler != null)
            {
                genreList = novelCrawler.GetGenres();
            }
            else
            {
                genreList = new List<Genre>();
            }
            RemovePlugin();

            return genreList;
        }

        public NovelContent GetNovelContent(string novelSlug, string chapterSlug)
        {
            LoadPlugins();
            NovelContent novelContent;
            if (novelCrawler != null)
            {
                novelContent = novelCrawler.GetNovelContent(novelSlug, chapterSlug);
            }
            else
            {
                novelContent= new NovelContent();
            }
            RemovePlugin();

            return novelContent;
        }

        public NovelDetail GetNovelDetail(string novelSlug)
        {
            LoadPlugins();
            NovelDetail novelDetail;
            if (novelCrawler != null)
            {
                novelDetail = novelCrawler.GetNovelDetail(novelSlug);
            }
            else
            {
                novelDetail= new NovelDetail();
            }
            RemovePlugin();

            return novelDetail;
        }
    }
}
