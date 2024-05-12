using Microsoft.AspNetCore.Mvc;
using NoshNovel.Plugin.Contexts.NovelCrawler;
using NoshNovel.Plugin.Contexts.NovelDownloader;
using NoshNovel.Models;
using NoshNovel.Plugin.Strategies.Utilities;

namespace NoshNovel.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NovelsController : ControllerBase
    {
        private readonly ILogger<NovelsController> logger;
        private readonly INovelCrawlerContext novelCrawlerContext;
        private readonly INovelDownloaderContext novelDownloaderContext;

        public NovelsController(ILogger<NovelsController> logger, INovelCrawlerContext novelCrawlerContext, 
            INovelDownloaderContext novelDownloaderContext)
        {
            this.logger = logger;
            this.novelCrawlerContext = novelCrawlerContext;
            this.novelDownloaderContext = novelDownloaderContext;
        }

        [HttpGet]
        [Route("search")]
        public IActionResult SearchByKeyword([FromQuery] string server, [FromQuery] string keyword,
            [FromQuery] int page = 1, [FromQuery] int perPage = 18)
        {
            novelCrawlerContext.SetNovelCrawlerStrategy(server);
            NovelSearchResult response = novelCrawlerContext.GetByKeyword(keyword, page, perPage);
            return Ok(response);
        }

        [HttpGet]
        [Route("genre-filter")]
        public IActionResult SearchByGenre([FromQuery] string server, [FromQuery] string genre,
            [FromQuery] int page = 1, [FromQuery] int perPage = 18)
        {
            novelCrawlerContext.SetNovelCrawlerStrategy(server);
            NovelSearchResult response = novelCrawlerContext.FilterByGenre(genre, page, perPage);
            return Ok(response);
        }

        [HttpGet]
        [Route("genres")]
        public IActionResult GetGenres([FromQuery] string server)
        {
            novelCrawlerContext.SetNovelCrawlerStrategy(server);
            IEnumerable<Genre> response = novelCrawlerContext.GetGenres();
            return Ok(response);
        }

        [HttpGet]
        [Route("servers")]
        public IActionResult GetServers()
        {
            IEnumerable<string> servers = novelCrawlerContext.GetNovelCrawlerServers();
            return Ok(servers);
        }

        [HttpGet]
        [Route("detail")]
        public IActionResult GetDetail([FromQuery] string server, [FromQuery] string novelSlug)
        {
            novelCrawlerContext.SetNovelCrawlerStrategy(server);
            NovelDetail novelDetail = novelCrawlerContext.GetNovelDetail(novelSlug);
            return Ok(novelDetail);
        }

        [HttpGet]
        [Route("chapters")]
        public IActionResult GetChapters([FromQuery] string server, [FromQuery] string novelSlug, int page = 1, int perPage = 40)
        {
            novelCrawlerContext.SetNovelCrawlerStrategy(server);
            NovelChaptersResult novelChaptersResult = novelCrawlerContext.GetChapterList(novelSlug, page, perPage);
            return Ok(novelChaptersResult);
        }

        [HttpGet]
        [Route("content")]
        public IActionResult GetContent([FromQuery] string server, [FromQuery] string novelSlug, string chapterSlug)
        {
            novelCrawlerContext.SetNovelCrawlerStrategy(server);
            NovelContent novelContent = novelCrawlerContext.GetNovelContent(novelSlug, chapterSlug);
            return Ok(novelContent);
        }

        [HttpGet]
        [Route("file-extensions")]
        public IActionResult GetDownloadFileExtensions()
        {
            IEnumerable<string> fileExtensions = novelDownloaderContext.GetFileExtensions();
            return Ok(fileExtensions);
        }

        // GET: api/download/{fileName}
        [HttpPost]
        [Route("download")]
        public async Task<IActionResult> DownloadNovel([FromBody] NovelDownloadRequest novelDownloadRequest)
        {
            novelCrawlerContext.SetNovelCrawlerStrategy(novelDownloadRequest.Server);
            novelDownloaderContext.SetNovelDownloaderStrategy(novelDownloadRequest.FileExtension);

            NovelDownloadObject novelDownloadObject = new NovelDownloadObject()
            {
                NovelDetail = novelCrawlerContext.GetNovelDetail(novelDownloadRequest.NovelSlug),
            };

            List<NovelContent> downloadChapters = new List<NovelContent>();
            foreach (var chapterSlug in novelDownloadRequest.ChapterSlugs)
            {
                NovelContent novelContent = novelCrawlerContext.GetNovelContent(novelDownloadRequest.NovelSlug, chapterSlug);
                downloadChapters.Add(novelContent);
            }
            novelDownloadObject.DownloadChapters = downloadChapters;
            novelDownloadObject.NovelStyling = novelDownloadRequest.NovelStyling;

            Stream novelFileStream = await novelDownloaderContext.GetFileStream(novelDownloadObject);
            
            string fileName = $"{HelperClass.GenerateSlug(novelDownloadObject.NovelDetail.Title)}.{novelDownloadRequest.FileExtension.ToLower()}";

            return File(novelFileStream, "application/octet-stream", fileName);
        }
    }
}
