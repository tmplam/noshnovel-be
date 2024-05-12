using NoshNovel.Models;

namespace NoshNovel.Plugin.Contexts.NovelCrawler
{
    public partial class NovelCrawlerContext : INovelCrawlerContext
    {
        public IEnumerable<string> GetNovelCrawlerServers()
        {
            List<string> servers = new List<string>();

            foreach (var dllFilePath in Directory.GetFiles(Path.Join(Directory.GetCurrentDirectory(),
                pluginPath), "*.dll"))
            {
                WeakReference assemblyWeakRef;
                var server = LoadServerName(dllFilePath, out assemblyWeakRef);
                servers.Add(server);

                for (int i = 0; assemblyWeakRef.IsAlive && i < 100; i++)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }
            }
            return servers;
        }

        public void SetNovelCrawlerStrategy(string novelServer)
        {
            this.novelServer = novelServer;
        }

        public async Task<NovelSearchResult> FilterByGenre(string genre, int page = 1, int perPage = 18)
        {
            LoadPlugins();
            NovelSearchResult novelSearchResult;
            if (novelCrawler != null)
            {
                novelSearchResult = await novelCrawler.FilterByGenre(genre, page, perPage);
            }
            else
            {
                novelSearchResult = new NovelSearchResult();
            }
            RemovePlugin();

            return novelSearchResult;
        }

        public async Task<NovelSearchResult> GetByKeyword(string keyword, int page = 1, int perPage = 18)
        {
            LoadPlugins();
            NovelSearchResult novelSearchResult;
            if (novelCrawler != null)
            {
                novelSearchResult = await novelCrawler.GetByKeyword(keyword, page, perPage);
            }
            else
            {
                novelSearchResult = new NovelSearchResult();
            }
            RemovePlugin();

            return novelSearchResult;
        }

        public async Task<NovelChaptersResult> GetChapterList(string novelSlug, int page = 1, int perPage = 40)
        {
            LoadPlugins();
            NovelChaptersResult novelChaptersResult;
            if (novelCrawler != null)
            {
                novelChaptersResult = await novelCrawler.GetChapterList(novelSlug, page, perPage);
            }
            else
            {
                novelChaptersResult = new NovelChaptersResult();
            }
            RemovePlugin();

            return novelChaptersResult;
        }

        public async Task<IEnumerable<Genre>> GetGenres()
        {
            LoadPlugins();
            IEnumerable<Genre> genreList;
            if (novelCrawler != null)
            {
                genreList = await novelCrawler.GetGenres();
            }
            else
            {
                genreList = new List<Genre>();
            }
            RemovePlugin();

            return genreList;
        }

        public async Task<NovelContent> GetNovelContent(string novelSlug, string chapterSlug)
        {
            LoadPlugins();
            NovelContent novelContent;
            if (novelCrawler != null)
            {
                novelContent = await novelCrawler.GetNovelContent(novelSlug, chapterSlug);
            }
            else
            {
                novelContent= new NovelContent();
            }
            RemovePlugin();

            return novelContent;
        }

        public async Task<NovelDetail> GetNovelDetail(string novelSlug)
        {
            LoadPlugins();
            NovelDetail novelDetail;
            if (novelCrawler != null)
            {
                novelDetail = await novelCrawler.GetNovelDetail(novelSlug);
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
