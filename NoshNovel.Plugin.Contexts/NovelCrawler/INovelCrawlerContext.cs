using NoshNovel.Plugin.Strategies;

namespace NoshNovel.Plugin.Contexts.NovelCrawler
{
    public interface INovelCrawlerContext : INovelCrawlerStrategy
    {
        IEnumerable<string> GetNovelCrawlerServers();
        void SetNovelCrawlerStrategy(string novelServer);
    }
}
