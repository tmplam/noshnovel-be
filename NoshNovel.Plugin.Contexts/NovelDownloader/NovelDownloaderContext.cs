using NoshNovel.Models;
using NoshNovel.Plugin.Strategies.Exeptions;
using System.Net;

namespace NoshNovel.Plugin.Contexts.NovelDownloader
{
    public partial class NovelDownloaderContext : INovelDownloaderContext
    {
        public IEnumerable<string> GetFileExtensions()
        {
            List<string> fileExtensions = new List<string>();

            foreach (var dllFilePath in Directory.GetFiles(Path.Join(Directory.GetCurrentDirectory(),
                pluginPath), "*.dll"))
            {
                WeakReference assemblyWeakRef;
                var fileExtension = LoadFileExtension(dllFilePath, out assemblyWeakRef);
                fileExtensions.Add(fileExtension);

                for (int i = 0; assemblyWeakRef.IsAlive && i < 100; i++)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }
            }
            return fileExtensions;
        }

        public void SetNovelDownloaderStrategy(string fileExtension)
        {
            this.fileExtension = fileExtension;
        }

        public async Task<Stream> GetFileStream(NovelDownloadObject novelDownloadObject)
        {
            LoadPlugins();
            Stream novelFileStream = Stream.Null;
            if (novelDownloader != null)
            {
                novelFileStream = await novelDownloader.GetFileStream(novelDownloadObject);
            }
            else
            {
                throw new RequestExeption(HttpStatusCode.NotFound, "File format not found!");
            }
            RemovePlugin();

            return novelFileStream;
        }
    }
}
