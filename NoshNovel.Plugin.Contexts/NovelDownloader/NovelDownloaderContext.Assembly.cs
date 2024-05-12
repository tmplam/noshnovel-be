using Microsoft.Extensions.Configuration;
using NoshNovel.Factories.AssemblyLoadContexts;
using NoshNovel.Models;
using NoshNovel.Plugin.Strategies;
using NoshNovel.Plugin.Strategies.Attributes;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace NoshNovel.Plugin.Contexts.NovelDownloader
{
    public partial class NovelDownloaderContext : INovelDownloaderContext
    {
        private string fileExtension;
        private readonly string pluginPath;
        // Reference to plugin novel downloader, should be removed after using
        private INovelDownloaderStrategy? novelDownloader = null;
        private WeakReference? weakReference = null;

        public NovelDownloaderContext(IConfiguration configuration)
        {
            fileExtension = string.Empty;
            pluginPath = configuration["PluginPaths:NovelDownloader"] ?? Directory.GetCurrentDirectory();

            if (pluginPath == null)
            {
                throw new Exception("Can't find downloader plugin path!");
            }
        }

        public void LoadPlugins()
        {
            foreach (var dllFilePath in Directory.GetFiles(Path.Join(Directory.GetCurrentDirectory(),
                pluginPath), "*.dll"))
            {
                var assemblyLoadContext = new CollectibleAssemblyLoadContext();
                Assembly assembly = assemblyLoadContext.LoadFromAssemblyPath(dllFilePath);

                weakReference = new WeakReference(assemblyLoadContext, trackResurrection: true);

                // Get all types in assembly
                var types = assembly.GetTypes();

                // Filter type
                var pluginTypes = types.Where(t => typeof(INovelDownloaderStrategy).IsAssignableFrom(t) && !t.IsInterface);
                bool foundDownloader = false;

                if (pluginTypes != null)
                {
                    foreach (var pluginType in pluginTypes)
                    {
                        DownloadFormatAttribute? downloadFormatAttribute = (DownloadFormatAttribute?)Attribute.GetCustomAttribute(pluginType,
                            typeof(DownloadFormatAttribute));

                        if (downloadFormatAttribute != null)
                        {
                            string novelExtension = downloadFormatAttribute.FileExtension;
                            if (string.Compare(novelExtension, fileExtension, true) == 0)
                            {
                                // Create instance
                                novelDownloader = Activator.CreateInstance(pluginType) as INovelDownloaderStrategy;
                                foundDownloader = true;
                                break;
                            }
                        }
                    }
                }
                assemblyLoadContext.Unload();

                // Check if novel crawler found
                if (foundDownloader)
                {
                    break;
                }
            }
        }

        public void RemovePlugin()
        {
            novelDownloader = null;
            if (weakReference != null)
            {
                for (int i = 0; weakReference.IsAlive && (i < 100); i++)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private string LoadFileExtension(string assemblyPath, out WeakReference assemblyWeakRef)
        {
            var assemblyLoadContext = new CollectibleAssemblyLoadContext();
            Assembly assembly = assemblyLoadContext.LoadFromAssemblyPath(assemblyPath);

            assemblyWeakRef = new WeakReference(assemblyLoadContext, trackResurrection: true);

            // Get all types in assembly
            var types = assembly.GetTypes();

            // Filter type
            var pluginTypes = types.Where(t => typeof(INovelDownloaderStrategy).IsAssignableFrom(t) && !t.IsInterface);

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
