using NoshNovel.Factories.AssemblyLoadContexts;
using NoshNovel.Models;
using NoshNovel.Plugins;
using NoshNovel.Plugins.Attributes;
using System.Reflection;

namespace NoshNovel.Factories.NovelDownloaders
{
    public partial class PluginNovelDownloader : INovelDownloader
    {
        private readonly string fileExtension;
        private readonly string pluginPath;
        // Reference to plugin novel downloader, should be removed after using
        private INovelDownloader? novelDownloader = null;
        private WeakReference? weakReference = null;

        public PluginNovelDownloader(string fileExtension, string pluginPath)
        {
            this.fileExtension = fileExtension;
            this.pluginPath = pluginPath;
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
                var pluginTypes = types.Where(t => typeof(INovelDownloader).IsAssignableFrom(t) && !t.IsInterface);
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
                                novelDownloader = Activator.CreateInstance(pluginType) as INovelDownloader;
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
    }
}
