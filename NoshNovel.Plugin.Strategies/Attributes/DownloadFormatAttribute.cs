namespace NoshNovel.Plugin.Strategies.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class DownloadFormatAttribute : Attribute
    {
        public string FileExtension { get; }

        public DownloadFormatAttribute(string fileExtension)
        {
            FileExtension = fileExtension;
        }
    }
}
