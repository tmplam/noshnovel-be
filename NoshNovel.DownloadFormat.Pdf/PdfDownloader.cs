using NoshNovel.Models;
using NoshNovel.Plugins;
using NoshNovel.Plugins.Attributes;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.IO;
using System.Text.RegularExpressions;

namespace NoshNovel.DownloadFormat.Pdf
{
    [DownloadFormat("pdf")]
    public class PdfDownloader : INovelDownloader
    {
        public async Task<Stream> GetFileStream(NovelDownloadObject novelDownloadObject)
        {
            var novelDetail = novelDownloadObject.NovelDetail;
            var downloadChapters = novelDownloadObject.DownloadChapters;
            var httpClient = new HttpClient();
            Stream imageStream = await httpClient.GetStreamAsync(novelDetail.CoverImage);

            MemoryStream stream = new MemoryStream();
            Document.Create(document =>
            {
                // Cover page
                document.Page(page =>
                {
                    page.MarginHorizontal(1.5f, Unit.Centimetre);
                    page.MarginVertical(2f, Unit.Centimetre);
                    page.PageColor(Colors.Lime.Lighten5);

                    page.Content()
                        .Column(column =>
                        {
                            // Title
                            column.Item().AlignCenter().Text(text =>
                            {
                                text.Span(novelDetail.Title).FontFamily("Times New Roman")
                                    .FontSize(30)
                                    .FontColor(Colors.Green.Darken2);
                            });

                            // Author
                            column.Item().PaddingTop(10).PaddingRight(20).AlignRight().Text(text =>
                            {
                                text.Span("Tác giả: ").FontFamily("Times New Roman")
                                    .FontSize(22)
                                    .FontColor(Colors.Red.Darken1);

                                text.Span(novelDetail.Author.Name).FontFamily("Times New Roman")
                                    .FontSize(18)
                                    .FontColor(Colors.Black);
                            });

                            // Genre
                            column.Item().PaddingVertical(10).PaddingRight(20).AlignRight().Text(text =>
                            {
                                text.Span("Thể loại: ").FontFamily("Times New Roman")
                                    .FontSize(22)
                                    .FontColor(Colors.Blue.Darken1);

                                for (var i = 0; i < novelDetail.Genres.Count(); i++)
                                {                                    
                                    text.Span(novelDetail.Genres.ElementAt(i).Name).FontFamily("Times New Roman")
                                        .FontSize(18)
                                        .FontColor(Colors.Black);
                                    if (i != novelDetail.Genres.Count() - 1)
                                    {
                                        text.Span(", ").FontFamily("Times New Roman")
                                        .FontSize(18)
                                        .FontColor(Colors.Black);
                                    }
                                }
                            });

                            //Cover image
                            column.Item().AlignMiddle().AlignCenter().Image(imageStream)
                                .WithCompressionQuality(ImageCompressionQuality.VeryHigh)
                                .WithRasterDpi(72)
                                .FitArea();
                        });
                });

                // Content pages
                foreach (var chapter in downloadChapters)
                {
                    string openTagPattern = @"<[^>/]+>";
                    string closeTagPattern = @"<\/[^>]+>";
                    string multipleLineBreakPattern = @"[\r\n]{3,}";

                    string content = chapter.Content
                        .Replace("<br>", "\r\n")
                        .Replace("&nbsp;", " ").Replace("&#160;", " ")
                        .Replace("&#8216;", "‘").Replace("&lsquo;", "‘")
                        .Replace("&#8217;", "’").Replace("&rsquo;", "’")
                        .Replace("&#8220;", "“").Replace("&ldquo;", "“")
                        .Replace("&#8221;", "”").Replace("&rdquo;", "”")
                        .Replace("&#34;", "”").Replace("&quot;", "\"")
                        .Replace("&#8230;", "...").Replace("&hellip;", "...");

                    content = Regex.Replace(content, openTagPattern, "");
                    content = Regex.Replace(content, closeTagPattern, "\r\n");
                    content = Regex.Replace(content, multipleLineBreakPattern, "\r\n\r\n");

                    string chapterLabel = chapter.Chapter.Label + (string.IsNullOrWhiteSpace(chapter.Chapter.Name) ? "" : $" - {chapter.Chapter.Name}");

                    document.Page(page =>
                    {
                        page.Margin(1.5f, Unit.Centimetre);
                        page.PageColor(Colors.Lime.Lighten5);

                        page.Content()
                            .Column(column =>
                            {
                                column.Item().AlignCenter().Text(text =>
                                {
                                    text.Span(chapterLabel).FontFamily("Times New Roman")
                                        .Bold()
                                        .FontSize(24)
                                        .FontColor(Colors.Green.Darken4);
                                    text.EmptyLine();
                                });

                                column.Item().Text(text =>
                                {
                                    text.Span(content).FontFamily("Times New Roman")
                                        .LineHeight(1.6f)
                                        .FontSize(20)
                                        .FontColor(Colors.Grey.Darken4);
                                });
                            });

                        page.Footer().BorderTop(1).Row(row =>
                        {
                            row.RelativeItem().Text(text =>
                            {
                                text.DefaultTextStyle(style => style.LineHeight(1.6f));
                                text.Span(chapterLabel).FontColor(Colors.Green.Darken4);
                            });

                            row.RelativeItem().AlignRight().Text(text =>
                            {
                                text.DefaultTextStyle(style => style.LineHeight(1.6f));
                                text.Span("Trang ");
                                text.CurrentPageNumber();
                                text.Span(" / ");
                                text.TotalPages();
                            });
                        });
                    });
                }
                
            }).GeneratePdf(stream);
            stream.Position = 0;
            return stream;
        }
    }
}
