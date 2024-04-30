using NoshNovel.Models;

namespace NoshNovel.Plugins
{
    public interface INovelDownloader
    {
        Stream GetFileStream(NovelDownloadObject novelDownloadObject);
    }
}
