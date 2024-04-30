using Microsoft.AspNetCore.Mvc;
using NoshNovel.Factories.NovelCrawlers;
using NoshNovel.Models;
using NoshNovel.Plugins;

namespace NoshNovel.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NovelsController : ControllerBase
    {
        private readonly ILogger<NovelsController> logger;
        private readonly INovelCrawlerFactory novelCrawlerFactory;

        public NovelsController(ILogger<NovelsController> logger, INovelCrawlerFactory novelCrawlerFactory)
        {
            this.logger = logger;
            this.novelCrawlerFactory = novelCrawlerFactory;
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

        // GET: api/download/{fileName}
        [HttpGet]
        [Route("download")]
        public IActionResult DownloadNovel([FromQuery] string server)
        {
            // Kiểm tra xem tập tin có tồn tại không
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "TextFile.txt");
            if (!System.IO.File.Exists(filePath))
            {
                return NotFound();
            }

            // Đọc nội dung của tập tin
            var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);

            // Trả về nội dung tập tin dưới dạng một phản hồi file
            return File(fileStream, "application/octet-stream", "filename.txt");
        }
    }
}
