using NoshNovel.Factories.AssemblyLoadContexts;
using NoshNovel.Plugins;
using NoshNovel.Plugins.Attributes;
using System.Reflection;

namespace NoshNovel.Factories.NovelCrawlers
{
    public partial class PluginNovelCrawler : INovelCrawler
    {
        private readonly string novelServer;
        private readonly string pluginPath;
        // Reference to plugin novel crawler, should be removed after using
        private INovelCrawler? novelCrawler = null;
        private WeakReference? weakReference = null;

        public PluginNovelCrawler(string novelServer, string pluginPath)
        {
            this.novelServer = novelServer;
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
                var pluginTypes = types.Where(t => typeof(INovelCrawler).IsAssignableFrom(t) && !t.IsInterface);
                bool foundCrawler = false;

                if (pluginTypes != null)
                {
                    foreach (var pluginType in pluginTypes)
                    {
                        NovelServerAttribute? novelServerAttribute = (NovelServerAttribute?) Attribute.GetCustomAttribute(pluginType, 
                            typeof(NovelServerAttribute));

                        if (novelServerAttribute != null)
                        {
                            string serverName = novelServerAttribute.HostName;
                            if (string.Compare(serverName, novelServer, true) == 0)
                            {
                                // Create instance
                                novelCrawler = Activator.CreateInstance(pluginType) as INovelCrawler;
                                foundCrawler = true;
                                break;
                            }
                        }
                    }
                }
                assemblyLoadContext.Unload();

                // Check if novel crawler found
                if (foundCrawler)
                {
                    break;
                }
            }
        }

        public void RemovePlugin()
        {
            novelCrawler = null;
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
