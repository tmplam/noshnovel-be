using Microsoft.AspNetCore.Mvc;
using NoshNovel.Factories.NovelCrawlers;
using NoshNovel.Factories.NovelDownloaders;
using NoshNovel.Models;
using NoshNovel.Plugins;
using NoshNovel.Plugins.Utilities;

namespace NoshNovel.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NovelsController : ControllerBase
    {
        private readonly ILogger<NovelsController> logger;
        private readonly INovelCrawlerFactory novelCrawlerFactory;
        private readonly INovelDownloaderFactory novelDownloaderFactory;

        public NovelsController(ILogger<NovelsController> logger, INovelCrawlerFactory novelCrawlerFactory, 
            INovelDownloaderFactory novelDownloaderFactory)
        {
            this.logger = logger;
            this.novelCrawlerFactory = novelCrawlerFactory;
            this.novelDownloaderFactory = novelDownloaderFactory;
        }

        [HttpGet]
        [Route("search")]
        public IActionResult SearchByKeyword([FromQuery] string server, [FromQuery] string keyword,
            [FromQuery] int page = 1, [FromQuery] int perPage = 18)
        {
            INovelCrawler novelCrawler = novelCrawlerFactory.CreateNovelCrawler(server);
            NovelSearchResult response = novelCrawler.GetByKeyword(keyword, page, perPage);
            return Ok(response);
        }

        [HttpGet]
        [Route("genre-filter")]
        public IActionResult SearchByGenre([FromQuery] string server, [FromQuery] string genre,
            [FromQuery] int page = 1, [FromQuery] int perPage = 18)
        {
            INovelCrawler novelCrawler = novelCrawlerFactory.CreateNovelCrawler(server);
            NovelSearchResult response = novelCrawler.FilterByGenre(genre, page, perPage);
            return Ok(response);
        }

        [HttpGet]
        [Route("genres")]
        public IActionResult GetGenres([FromQuery] string server)
        {
            INovelCrawler novelCrawler = novelCrawlerFactory.CreateNovelCrawler(server);
            IEnumerable<Genre> response = novelCrawler.GetGenres();
            return Ok(response);
        }

        [HttpGet]
        [Route("servers")]
        public IActionResult GetServers()
        {
            IEnumerable<string> servers = novelCrawlerFactory.GetNovelCrawlerServers();
            return Ok(servers);
        }

        [HttpGet]
        [Route("detail")]
        public IActionResult GetDetail([FromQuery] string server, [FromQuery] string novelSlug)
        {
            INovelCrawler novelCrawler = novelCrawlerFactory.CreateNovelCrawler(server);
            NovelDetail novelDetail = novelCrawler.GetNovelDetail(novelSlug);
            return Ok(novelDetail);
        }

        [HttpGet]
        [Route("chapters")]
        public IActionResult GetChapters([FromQuery] string server, [FromQuery] string novelSlug, int page = 1, int perPage = 40)
        {
            INovelCrawler novelCrawler = novelCrawlerFactory.CreateNovelCrawler(server);
            NovelChaptersResult novelChaptersResult = novelCrawler.GetChapterList(novelSlug, page, perPage);
            return Ok(novelChaptersResult);
        }

        [HttpGet]
        [Route("content")]
        public IActionResult GetContent([FromQuery] string server, [FromQuery] string novelSlug, string chapterSlug)
        {
            INovelCrawler novelCrawler = novelCrawlerFactory.CreateNovelCrawler(server);
            NovelContent novelContent = novelCrawler.GetNovelContent(novelSlug, chapterSlug);
            return Ok(novelContent);
        }

        [HttpGet]
        [Route("file-extensions")]
        public IActionResult GetDownloadFileExtensions()
        {
            IEnumerable<string> fileExtensions = novelDownloaderFactory.GetFileExtensions();
            return Ok(fileExtensions);
        }

        // GET: api/download/{fileName}
        [HttpPost]
        [Route("download")]
        public async Task<IActionResult> DownloadNovel([FromBody] NovelDownloadRequest novelDownloadRequest)
        {
            INovelCrawler novelCrawler = novelCrawlerFactory.CreateNovelCrawler(novelDownloadRequest.Server);
            INovelDownloader novelDownloader = novelDownloaderFactory.CreateNovelDownloader(novelDownloadRequest.FileExtension);

            NovelDownloadObject novelDownloadObject = new NovelDownloadObject()
            {
                NovelDetail = novelCrawler.GetNovelDetail(novelDownloadRequest.NovelSlug),
            };

            List<NovelContent> downloadChapters = new List<NovelContent>();
            foreach (var chapterSlug in novelDownloadRequest.ChapterSlugs)
            {
                NovelContent novelContent = novelCrawler.GetNovelContent(novelDownloadRequest.NovelSlug, chapterSlug);
                downloadChapters.Add(novelContent);
            }
            novelDownloadObject.DownloadChapters = downloadChapters;
            novelDownloadObject.NovelStyling = novelDownloadRequest.NovelStyling;

            Stream novelFileStream = await novelDownloader.GetFileStream(novelDownloadObject);
            
            string fileName = $"{HelperClass.GenerateSlug(novelDownloadObject.NovelDetail.Title)}.{novelDownloadRequest.FileExtension.ToLower()}";

            return File(novelFileStream, "application/octet-stream", fileName);
        }
    }
}
