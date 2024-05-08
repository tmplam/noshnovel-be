namespace NoshNovel.API.Notifications.FileWatcherService
{
    public interface IPluginWatcher
    {
        void InitializeNovelServerPluginWatcher();
        void InitializeDownloadFormatPluginWatcher();

        void StartWatcher();
    }
}
