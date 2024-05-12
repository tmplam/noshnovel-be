using NoshNovel.Plugin.Strategies;

namespace NoshNovel.Plugin.Contexts.NovelDownloader
{
    public interface INovelDownloaderContext : INovelDownloaderStrategy
    {
        IEnumerable<string> GetFileExtensions();
        void SetNovelDownloaderStrategy(string fileExtension);
    }
}
