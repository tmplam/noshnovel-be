using Microsoft.AspNetCore.Mvc;
using NoshNovel.Models;
using NoshNovel.Servers.TruyenFull;

namespace NoshNovel.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NovelsController : ControllerBase
    {
        public NovelsController()
        {
            
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
            return Ok();
        }

        [HttpGet]
        [Route("servers")]
        public IActionResult GetServers()
        {
            return Ok();
        }

        [HttpGet]
        [Route("detail")]
        public IActionResult GetDetail([FromQuery] string server, [FromQuery] string url)
        {
            return Ok();
        }
    }
}
