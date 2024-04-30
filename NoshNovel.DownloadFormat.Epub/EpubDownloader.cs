using NoshNovel.Models;
using NoshNovel.Plugins;
using NoshNovel.Plugins.Attributes;

namespace NoshNovel.DownloadFormat.Epub
{
    [DownloadFormat("epub")]
    public class EpubDownloader : INovelDownloader
    {
        public Stream GetFileStream(NovelDownloadObject novelDownloadObject)
        {
            var novelDetail = novelDownloadObject.NovelDetail;
            var downloadChapters = novelDownloadObject.DownloadChapters;

            // Create an Epub instance
            var doc = new QuickEPUB.Epub(novelDetail.Title, novelDetail.Author.Name);

            using (var jpgStream = new FileStream("image.jpg", FileMode.Open))
            {
                doc.AddResource("cover.jpg", QuickEPUB.EpubResourceType.JPEG, jpgStream);
            }

            if (downloadChapters != null)
            {
                foreach (var chapter in downloadChapters)
                {
                    doc.AddSection($"{chapter.Chapter.Label} - {chapter.Chapter.Name}", 
                        $"<div style=\"{novelDownloadObject.NovelStyling}\">{chapter.Content}</div>");
                }
            }

            // Export the result
            MemoryStream stream = new MemoryStream();
            doc.Export(stream);

            return stream;
        }
    }
}
