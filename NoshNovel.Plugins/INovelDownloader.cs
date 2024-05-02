using NoshNovel.Models;

namespace NoshNovel.Plugins
{
    public interface INovelDownloader
    {
        Task<Stream> GetFileStream(NovelDownloadObject novelDownloadObject);
    }
}
