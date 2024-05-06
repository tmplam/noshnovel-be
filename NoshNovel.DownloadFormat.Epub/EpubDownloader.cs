using NoshNovel.Models;
using NoshNovel.Plugins;
using NoshNovel.Plugins.Attributes;

namespace NoshNovel.DownloadFormat.Epub
{
    [DownloadFormat("epub")]
    public class EpubDownloader : INovelDownloader
    {
        public async Task<Stream> GetFileStream(NovelDownloadObject novelDownloadObject)
        {
            var novelDetail = novelDownloadObject.NovelDetail;
            var downloadChapters = novelDownloadObject.DownloadChapters;

            // Create an Epub instance
            var doc = new QuickEPUB.Epub(novelDetail.Title, novelDetail.Author.Name);

            // Add cover image
            using (var httpClient = new HttpClient())
            {
                using (Stream imageStream = await httpClient.GetStreamAsync(novelDetail.CoverImage))
                {
                    doc.AddResource("cover.jpg", QuickEPUB.EpubResourceType.JPEG, imageStream, isCover: true);
                }
            }

            if (downloadChapters != null)
            {
                foreach (var chapter in downloadChapters)
                {
                    string chapterLabel = chapter.Chapter.Label + (string.IsNullOrWhiteSpace(chapter.Chapter.Name) ? "" : $" - {chapter.Chapter.Name}");
                    
                    string chapterContent = chapter.Content
                        .Replace("<br>", "\r\n")
                        .Replace("&nbsp;", " ").Replace("&#160;", " ")
                        .Replace("&#180;", "´").Replace("&acute;", "´")
                        .Replace("&#8216;", "‘").Replace("&lsquo;", "‘")
                        .Replace("&#8217;", "’").Replace("&rsquo;", "’")
                        .Replace("&#8220;", "“").Replace("&ldquo;", "“")
                        .Replace("&#8221;", "”").Replace("&rdquo;", "”")
                        .Replace("&#8242;", "′").Replace("&prime;", "′")
                        .Replace("&#8243;", "″").Replace("&Prime;", "″")
                        .Replace("&#34;", "”").Replace("&quot;", "\"")
                        .Replace("&#39;", "'").Replace("&apos;", "'")
                        .Replace("&#8230;", "...").Replace("&hellip;", "...");

                    doc.AddSection($"{chapterLabel}", $"""
                        <h2 style="text-align: center; color: green; margin-top: 0">{chapterLabel}</h2>
                        <div style="white-space: pre-wrap; white-space-collapse: preserve; line-height: 160%">{chapterContent}</div>
                        """);
                }
            }

            // Export the result
            MemoryStream stream = new MemoryStream();
            doc.Export(stream);

            MemoryStream copyStream = new MemoryStream(stream.ToArray());
            copyStream.Position = 0;

            return copyStream;
        }
    }
}
