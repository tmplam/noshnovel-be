using Microsoft.Extensions.Configuration;
using NoshNovel.Factories.AssemblyLoadContexts;
using NoshNovel.Plugin.Strategies;
using NoshNovel.Plugin.Strategies.Attributes;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace NoshNovel.Plugin.Contexts.NovelCrawler
{
    public partial class NovelCrawlerContext : INovelCrawlerContext
    {
        private string novelServer;
        private readonly string pluginPath;
        // Reference to plugin novel crawler, should be removed after using
        private INovelCrawlerStrategy? novelCrawler = null;
        private WeakReference? weakReference = null;

        public NovelCrawlerContext(IConfiguration configuration)
        {
            novelServer = string.Empty;
            pluginPath = configuration["PluginPaths:NovelServer"] ?? string.Empty;
            // Compatible with all operating sysrem
            pluginPath = pluginPath.Replace('/', Path.DirectorySeparatorChar);

            if (string.IsNullOrEmpty(pluginPath))
            {
                throw new Exception("Can't find server plugin path!");
            }
        }

        private void LoadPlugins()
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
                var pluginTypes = types.Where(t => typeof(INovelCrawlerStrategy).IsAssignableFrom(t) && !t.IsInterface);
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
                                novelCrawler = Activator.CreateInstance(pluginType) as INovelCrawlerStrategy;
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

        private void RemovePlugin()
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

        [MethodImpl(MethodImplOptions.NoInlining)]
        private string LoadServerName(string assemblyPath, out WeakReference assemblyWeakRef)
        {
            var assemblyLoadContext = new CollectibleAssemblyLoadContext();
            Assembly assembly = assemblyLoadContext.LoadFromAssemblyPath(assemblyPath);

            assemblyWeakRef = new WeakReference(assemblyLoadContext, trackResurrection: true);

            // Get all types in assembly
            var types = assembly.GetTypes();

            // Filter type
            var pluginTypes = types.Where(t => typeof(INovelCrawlerStrategy).IsAssignableFrom(t) && !t.IsInterface);

            string serverName = string.Empty;
            if (pluginTypes != null)
            {
                foreach (var pluginType in pluginTypes)
                {
                    NovelServerAttribute? novelServerAttribute = (NovelServerAttribute?)Attribute.GetCustomAttribute(pluginType,
                        typeof(NovelServerAttribute));

                    if (novelServerAttribute != null)
                    {
                        serverName = novelServerAttribute.HostName;
                        break;
                    }
                }
            }

            assemblyLoadContext.Unload();
            return serverName;
        }
    }
}
