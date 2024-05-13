using NoshNovel.Models;

namespace NoshNovel.Plugin.Strategies
{
    public interface INovelDownloaderStrategy
    {
        Task<Stream> GetFileStream(NovelDownloadObject novelDownloadObject);
    }
}
