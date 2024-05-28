using Microsoft.AspNetCore.SignalR;

namespace NoshNovel.API.Notifications.FileWatcherService
{
    public class PluginWatcher : IPluginWatcher
    {
        private readonly IHubContext<ServiceUpdateHub> hubContext;
        private readonly IConfiguration configuration;
        private readonly FileSystemWatcher novelServerPluginWatcher;
        private readonly FileSystemWatcher downloadFormatPluginWatcher;

        public PluginWatcher(IHubContext<ServiceUpdateHub> hubContext, IConfiguration configuration)
        {
            this.hubContext = hubContext;
            this.configuration = configuration;

            novelServerPluginWatcher = new FileSystemWatcher();
            downloadFormatPluginWatcher = new FileSystemWatcher();
        }

        public void InitializeNovelServerPluginWatcher()
        {
            if (configuration["PluginPaths:NovelServer"] != null)
            {
                novelServerPluginWatcher.Path = Path.Join(Directory.GetCurrentDirectory(), configuration["PluginPaths:NovelServer"]);
            }
            else
            {
                novelServerPluginWatcher.Path = Directory.GetCurrentDirectory();
            }

            novelServerPluginWatcher.NotifyFilter = NotifyFilters.Attributes
                                 | NotifyFilters.CreationTime
                                 | NotifyFilters.DirectoryName
                                 | NotifyFilters.FileName
                                 | NotifyFilters.LastAccess
                                 | NotifyFilters.LastWrite
                                 | NotifyFilters.Security
                                 | NotifyFilters.Size;
            novelServerPluginWatcher.Filter = "*.dll";

            novelServerPluginWatcher.Changed += NotifyOnNovelServerUpdate;
            novelServerPluginWatcher.Created += NotifyOnNovelServerUpdate;
            novelServerPluginWatcher.Deleted += NotifyOnNovelServerUpdate;
            novelServerPluginWatcher.Renamed += NotifyOnNovelServerUpdate;


            novelServerPluginWatcher.EnableRaisingEvents = true;
        }

        private async void NotifyOnNovelServerUpdate(object sender, FileSystemEventArgs e)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Server Update!!!");
            Console.ResetColor();
            await hubContext.Clients.All.SendAsync("NovelServerUpdate");
        }

        public void InitializeDownloadFormatPluginWatcher()
        {
            if (configuration["PluginPaths:NovelDownloader"] != null)
            {
                downloadFormatPluginWatcher.Path = Path.Join(Directory.GetCurrentDirectory(), configuration["PluginPaths:NovelDownloader"]);
            }
            else
            {
                downloadFormatPluginWatcher.Path = Directory.GetCurrentDirectory();
            }
            downloadFormatPluginWatcher.NotifyFilter = NotifyFilters.Attributes
                                 | NotifyFilters.CreationTime
                                 | NotifyFilters.DirectoryName
                                 | NotifyFilters.FileName
                                 | NotifyFilters.LastAccess
                                 | NotifyFilters.LastWrite
                                 | NotifyFilters.Security
                                 | NotifyFilters.Size;
            novelServerPluginWatcher.Filter = "*.dll";

            downloadFormatPluginWatcher.Changed += NotifyOnDownloadFormatUpdate;
            downloadFormatPluginWatcher.Deleted += NotifyOnDownloadFormatUpdate;
            downloadFormatPluginWatcher.Created += NotifyOnDownloadFormatUpdate;
            downloadFormatPluginWatcher.Renamed += NotifyOnDownloadFormatUpdate;

            downloadFormatPluginWatcher.EnableRaisingEvents = true;
        }

        private async void NotifyOnDownloadFormatUpdate(object sender, FileSystemEventArgs e)
        {
            await hubContext.Clients.All.SendAsync("DownloadFormatUpdate");
        }

        public void StartWatcher()
        {
            InitializeNovelServerPluginWatcher();
            InitializeDownloadFormatPluginWatcher();
        }
    }
}
