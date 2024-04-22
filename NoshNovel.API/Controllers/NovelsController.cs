using Microsoft.AspNetCore.Mvc;

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
        public IActionResult Search([FromQuery] string keyword)
        {

            return Ok();
        }

        [HttpGet]
        [Route("genres")]
        public IActionResult GetGenres([FromQuery] string server)
        {

            return Ok();
        }

        [HttpGet]
        [Route("server")]
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
