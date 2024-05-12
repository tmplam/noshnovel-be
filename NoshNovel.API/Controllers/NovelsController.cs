using Microsoft.AspNetCore.Mvc;
using NoshNovel.Plugin.Contexts.NovelCrawler;
using NoshNovel.Plugin.Contexts.NovelDownloader;
using NoshNovel.Models;
using NoshNovel.Plugin.Strategies.Utilities;
using NoshNovel.Server.TruyenChuStrategy;

namespace NoshNovel.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NovelsController : ControllerBase
    {
        private readonly ILogger<NovelsController> logger;
        private readonly INovelCrawlerContext novelCrawlerContext;
        private readonly INovelDownloaderContext novelDownloaderContext;
        TruyenChuCrawlerStrategy truyenChuCrawlerStrategy { get; set; }

        public NovelsController(ILogger<NovelsController> logger, INovelCrawlerContext novelCrawlerContext, 
            INovelDownloaderContext novelDownloaderContext)
        {
            this.logger = logger;
            this.novelCrawlerContext = novelCrawlerContext;
            this.novelDownloaderContext = novelDownloaderContext;
            truyenChuCrawlerStrategy = new();
        }

        [HttpGet]
        [Route("search")]
        public async Task<IActionResult> SearchByKeyword([FromQuery] string server, [FromQuery] string keyword,
            [FromQuery] int page = 1, [FromQuery] int perPage = 18)
        {
            novelCrawlerContext.SetNovelCrawlerStrategy(server);
            NovelSearchResult response = await novelCrawlerContext.GetByKeyword(keyword, page, perPage);
            return Ok(response);
        }

        [HttpGet]
        [Route("genre-filter")]
        public async Task<IActionResult> SearchByGenre([FromQuery] string server, [FromQuery] string genre,
            [FromQuery] int page = 1, [FromQuery] int perPage = 18)
        {
            novelCrawlerContext.SetNovelCrawlerStrategy(server);
            NovelSearchResult response = await novelCrawlerContext.FilterByGenre(genre, page, perPage);
            return Ok(response);
        }

        [HttpGet]
        [Route("genres")]
        public async Task<IActionResult> GetGenres([FromQuery] string server)
        {
            novelCrawlerContext.SetNovelCrawlerStrategy(server);
            IEnumerable<Genre> response = await novelCrawlerContext.GetGenres();
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
        public async Task<IActionResult> GetDetail([FromQuery] string server, [FromQuery] string novelSlug)
        {
            novelCrawlerContext.SetNovelCrawlerStrategy(server);
            NovelDetail novelDetail = await novelCrawlerContext.GetNovelDetail(novelSlug);
            return Ok(novelDetail);
        }

        [HttpGet]
        [Route("chapters")]
        public async Task<IActionResult> GetChapters([FromQuery] string server, [FromQuery] string novelSlug, int page = 1, int perPage = 40)
        {
            novelCrawlerContext.SetNovelCrawlerStrategy(server);
            NovelChaptersResult novelChaptersResult = await novelCrawlerContext.GetChapterList(novelSlug, page, perPage);
            return Ok(novelChaptersResult);
        }

        [HttpGet]
        [Route("content")]
        public async Task<IActionResult> GetContent([FromQuery] string server, [FromQuery] string novelSlug, string chapterSlug)
        {
            novelCrawlerContext.SetNovelCrawlerStrategy(server);
            NovelContent novelContent = await truyenChuCrawlerStrategy.GetNovelContent(novelSlug, chapterSlug);
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
                NovelDetail = await novelCrawlerContext.GetNovelDetail(novelDownloadRequest.NovelSlug),
            };

            List<NovelContent> downloadChapters = new List<NovelContent>();
            foreach (var chapterSlug in novelDownloadRequest.ChapterSlugs)
            {
                NovelContent novelContent = await novelCrawlerContext.GetNovelContent(novelDownloadRequest.NovelSlug, chapterSlug);
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
