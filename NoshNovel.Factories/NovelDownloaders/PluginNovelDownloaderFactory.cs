using Microsoft.Extensions.Configuration;
using NoshNovel.Factories.AssemblyLoadContexts;
using NoshNovel.Plugins;
using NoshNovel.Plugins.Attributes;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace NoshNovel.Factories.NovelDownloaders
{
    public class PluginNovelDownloaderFactory : INovelDownloaderFactory
    {
        private readonly string pluginPath;

        public PluginNovelDownloaderFactory(IConfiguration configuration)
        {
            pluginPath = configuration["PluginPaths:NovelDownloader"] ?? Directory.GetCurrentDirectory();

            if (pluginPath == null)
            {
                throw new Exception("Can't find downloader plugin path!");
            }
        }

        public INovelDownloader CreateNovelDownloader(string fileExtension)
        {
            INovelDownloader novelDownloader = new PluginNovelDownloader(fileExtension, pluginPath);
            return novelDownloader;
        }

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

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static string LoadFileExtension(string assemblyPath, out WeakReference assemblyWeakRef)
        {
            var assemblyLoadContext = new CollectibleAssemblyLoadContext();
            Assembly assembly = assemblyLoadContext.LoadFromAssemblyPath(assemblyPath);

            assemblyWeakRef = new WeakReference(assemblyLoadContext, trackResurrection: true);

            // Get all types in assembly
            var types = assembly.GetTypes();

            // Filter type
            var pluginTypes = types.Where(t => typeof(INovelDownloader).IsAssignableFrom(t) && !t.IsInterface);

            string fileExtension = string.Empty;
            if (pluginTypes != null)
            {
                foreach (var pluginType in pluginTypes)
                {
                    DownloadFormatAttribute? downloadFormatAttribute = (DownloadFormatAttribute?)Attribute.GetCustomAttribute(pluginType,
                        typeof(DownloadFormatAttribute));

                    if (downloadFormatAttribute != null)
                    {
                        fileExtension = downloadFormatAttribute.FileExtension;
                        break;
                    }
                }
            }

            assemblyLoadContext.Unload();
            return fileExtension;
        }
    }
}
