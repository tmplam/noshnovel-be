using NoshNovel.Models;
using NoshNovel.Plugins;

namespace NoshNovel.Factories.NovelDownloaders
{
    public partial class PluginNovelDownloader : INovelDownloader
    {
        public Stream GetFileStream(NovelDownloadObject novelDownloadObject)
        {
            LoadPlugins();
            Stream novelFileStream = Stream.Null;
            if (novelDownloader != null)
            {
                novelFileStream = novelDownloader.GetFileStream(novelDownloadObject);
            }
            RemovePlugin();

            return novelFileStream;
        }
    }
}
