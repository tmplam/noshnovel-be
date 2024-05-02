using NoshNovel.Plugins;

namespace NoshNovel.Factories.NovelDownloaders
{
    public interface INovelDownloaderFactory
    {
        IEnumerable<string> GetFileExtensions();
        INovelDownloader CreateNovelDownloader(string fileExtension);
    }
}
