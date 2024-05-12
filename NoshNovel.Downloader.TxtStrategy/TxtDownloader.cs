using NoshNovel.Models;
using NoshNovel.Plugin.Strategies;
using NoshNovel.Plugin.Strategies.Attributes;
using System.Text;
using System.Text.RegularExpressions;

namespace NoshNovel.Downloader.TxtStrategy
{
    [DownloadFormat("txt")]
    public class TxtDownloader : INovelDownloaderStrategy
    {
        public async Task<Stream> GetFileStream(NovelDownloadObject novelDownloadObject)
        {
            var novelDetail = novelDownloadObject.NovelDetail;
            var downloadChapters = novelDownloadObject.DownloadChapters;

            StringBuilder novelContent = new StringBuilder();
            novelContent.AppendLine($"****** <<< {novelDetail.Title.ToUpper()} >>>");
            novelContent.AppendLine($"****** ✍️ TÁC GIẢ: {novelDetail.Author.Name}");
            novelContent.AppendLine($"****** 📙 THỂ LOẠI: {novelDetail.Genres?.Select(genre => genre.Name)
                .Aggregate((acc, name) => acc + " - " + name)}");

            foreach (var chapter in downloadChapters)
            {
                novelContent.AppendLine();
                novelContent.AppendLine();
                novelContent.AppendLine();

                string chapterLabel = chapter.Chapter.Label + (string.IsNullOrWhiteSpace(chapter.Chapter.Name) ? "" : $" - {chapter.Chapter.Name}");
                novelContent.AppendLine($"✅ {chapterLabel.ToUpper()}");
                novelContent.AppendLine($"============================================================");
                novelContent.AppendLine();

                string openTagPattern = @"<[^>/]+>";
                string closeTagPattern = @"<\/[^>]+>";
                string multipleLineBreakPattern = @"[\r\n]{3,}";

                string content = chapter.Content
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

                content = Regex.Replace(content, openTagPattern, "");
                content = Regex.Replace(content, closeTagPattern, Environment.NewLine);

                content = Regex.Replace(content, multipleLineBreakPattern, $"{Environment.NewLine}{Environment.NewLine}");
                content = Regex.Replace(content, @"(?<!\r?\n)\r?\n(?!\r?\n)", $"{Environment.NewLine}{Environment.NewLine}");
                novelContent.AppendLine(content);
            }

            MemoryStream novelStream = new MemoryStream();

            byte[] buffer = Encoding.UTF8.GetBytes(novelContent.ToString());
            await novelStream.WriteAsync(buffer, 0, buffer.Length);
            novelStream.Position = 0;

            return novelStream;
        }
    }
}