using System.Reflection;
using System.Runtime.CompilerServices;
using NoshNovel.Factories.AssemblyLoadContexts;
using Microsoft.Extensions.Configuration;
using NoshNovel.Plugins.Attributes;
using NoshNovel.Plugins;


namespace NoshNovel.Factories.NovelCrawlers
{
    public class PluginNovelCrawlerFactory : INovelCrawlerFactory
    {
        private readonly string pluginPath;

        public PluginNovelCrawlerFactory(IConfiguration configuration)
        {
            pluginPath = configuration["PluginPaths:NovelServer"];

            if (pluginPath == null)
            {
                throw new Exception("Can't find server plugin path!");
            }
        }

        public INovelCrawler CreateNovelCrawler(string novelServer)
        {
            INovelCrawler novelCrawler = new PluginNovelCrawler(novelServer, pluginPath);
            return novelCrawler;
        }

        public IEnumerable<string> GetNovelCrawlerServers()
        {
            List<string> servers = new List<string>();

            foreach (var dllFilePath in Directory.GetFiles(Path.Join(Directory.GetCurrentDirectory(),
                pluginPath), "*.dll"))
            {
                WeakReference assemblyWeakRef;
                var server = LoadServerName(dllFilePath, out assemblyWeakRef);
                servers.Add(server);

                for (int i = 0; assemblyWeakRef.IsAlive && i < 100; i++)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }
            }
            return servers;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static string LoadServerName(string assemblyPath, out WeakReference assemblyWeakRef)
        {
            var assemblyLoadContext = new CollectibleAssemblyLoadContext();
            Assembly assembly = assemblyLoadContext.LoadFromAssemblyPath(assemblyPath);

            assemblyWeakRef = new WeakReference(assemblyLoadContext, trackResurrection: true);

            // Get all types in assembly
            var types = assembly.GetTypes();

            // Filter type
            var pluginTypes = types.Where(t => typeof(INovelCrawler).IsAssignableFrom(t) && !t.IsInterface);

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
