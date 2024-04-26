using Microsoft.AspNetCore.Mvc;
using NoshNovel.Models;
using NoshNovel.Servers.TruyenFull;

namespace NoshNovel.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NovelsController : ControllerBase
    {
        private readonly ILogger<NovelsController> logger;

        public NovelsController(ILogger<NovelsController> logger)
        {
            this.logger = logger;
        }

        [HttpGet]
        [Route("search")]
        public IActionResult SearchByKeyword([FromQuery] string server, [FromQuery] string keyword,
            [FromQuery] int page = 1, [FromQuery] int perPage = 18)
        {
            TruyenFullCrawler crawler = new TruyenFullCrawler();
            NovelSearchResult response = crawler.GetByKeyword(keyword, page, perPage);
            return Ok(response);
        }

        [HttpGet]
        [Route("genre-filter")]
        public IActionResult SearchByGenre([FromQuery] string server, [FromQuery] string genre,
            [FromQuery] int page = 1, [FromQuery] int perPage = 18)
        {
            TruyenFullCrawler crawler = new TruyenFullCrawler();
            NovelSearchResult response = crawler.FilterByGenre(genre, page, perPage);
            return Ok(response);
        }

        [HttpGet]
        [Route("genres")]
        public IActionResult GetGenres([FromQuery] string server)
        {
            TruyenFullCrawler crawler = new TruyenFullCrawler();
            return Ok(crawler.GetGenres());
        }

        [HttpGet]
        [Route("servers")]
        public IActionResult GetServers()
        {
            TruyenFullCrawler crawler = new TruyenFullCrawler();
            return Ok(crawler.GetGenres());
        }

        [HttpGet]
        [Route("detail")]
        public IActionResult GetDetail([FromQuery] string server, [FromQuery] string novelSlug)
        {
            TruyenFullCrawler crawler = new TruyenFullCrawler();
            return Ok(crawler.GetNovelDetail(novelSlug));
        }

        [HttpGet]
        [Route("chapters")]
        public IActionResult GetChapters([FromQuery] string server, [FromQuery] string novelSlug, int page = 1, int perPage = 40)
        {
            TruyenFullCrawler crawler = new TruyenFullCrawler();
            return Ok(crawler.GetChapterList(novelSlug, page, perPage));
        }

        [HttpGet]
        [Route("content")]
        public IActionResult GetContent([FromQuery] string server, [FromQuery] string novelSlug, string chapterSlug)
        {
            TruyenFullCrawler crawler = new TruyenFullCrawler();
            return Ok(crawler.GetNovelContent(novelSlug, chapterSlug));
        }
    }
}
