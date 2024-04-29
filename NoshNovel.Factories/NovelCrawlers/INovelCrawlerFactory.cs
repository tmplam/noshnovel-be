using NoshNovel.Plugins;

namespace NoshNovel.Factories.NovelCrawlers
{
    public interface INovelCrawlerFactory
    {
        IEnumerable<string> GetNovelCrawlerServers();
        INovelCrawler CreateNovelCrawler(string novelServer);
    }
}
