using Microsoft.Extensions.Logging;
using NoshNovel.API.Controllers;
using NoshNovel.Plugin.Contexts.NovelCrawler;
using NoshNovel.Plugin.Contexts.NovelDownloader;
using FakeItEasy;
using NoshNovel.Models;
using Microsoft.AspNetCore.Mvc;
using FluentAssertions;

namespace NoshNovel.Tests.Controllers
{
    public class NovelsControllerTests
    {
        private readonly INovelCrawlerContext novelCrawlerContext;
        private readonly INovelDownloaderContext novelDownloaderContext;
        private readonly ILogger<NovelsController> logger;

        public NovelsControllerTests()
        {
            novelCrawlerContext = A.Fake<INovelCrawlerContext>();
            novelDownloaderContext = A.Fake<INovelDownloaderContext>();
            logger = A.Fake<ILogger<NovelsController>>();
        }

        [Theory]
        [InlineData("truyenfull.vn", "thiên tôn", 1, 20)]
        public async Task NovelsController_SearchByKeyword_ReturnsOK(string server, string keyword, int page, int perPage)
        {
            //Arrange
            var novelSearchResult = A.Fake<NovelSearchResult>();
            A.CallTo(() => novelCrawlerContext.GetByKeyword(keyword, page, perPage)).Returns(novelSearchResult);
            var controller = new NovelsController(logger, novelCrawlerContext, novelDownloaderContext);

            //Act
            var response = await controller.SearchByKeyword(server, keyword, page, perPage);

            //Assert
            response.Should().NotBeNull();
            response.Should().BeOfType(typeof(OkObjectResult));
        }

        [Theory]
        [InlineData("truyenfull.vn", "ngon-tinh", 1, 20)]
        public async Task NovelsController_SearchByGenre_ReturnsOK(string server, string genre, int page, int perPage)
        {
            //Arrange
            var novelSearchResult = A.Fake<NovelSearchResult>();
            A.CallTo(() => novelCrawlerContext.FilterByGenre(genre, page, perPage)).Returns(novelSearchResult);
            var controller = new NovelsController(logger, novelCrawlerContext, novelDownloaderContext);

            //Act
            var response = await controller.SearchByGenre(server, genre, page, perPage);

            //Assert
            response.Should().NotBeNull();
            response.Should().BeOfType(typeof(OkObjectResult));
        }

        [Theory]
        [InlineData("truyenfull.vn")]
        public async Task NovelsController_GetGenres_ReturnsOK(string server)
        {
            //Arrange
            var genres = A.Fake<IEnumerable<Genre>>();
            A.CallTo(() => novelCrawlerContext.GetGenres()).Returns(genres);
            var controller = new NovelsController(logger, novelCrawlerContext, novelDownloaderContext);

            //Act
            var response = await controller.GetGenres(server);

            //Assert
            response.Should().NotBeNull();
            response.Should().BeOfType(typeof(OkObjectResult));
        }

        [Fact]
        public void NovelsController_GetServers_ReturnsOK()
        {
            //Arrange
            var servers = A.Fake<IEnumerable<string>>();
            A.CallTo(() => novelCrawlerContext.GetNovelCrawlerServers()).Returns(servers);
            var controller = new NovelsController(logger, novelCrawlerContext, novelDownloaderContext);

            //Act
            var response = controller.GetServers();

            //Assert
            response.Should().NotBeNull();
            response.Should().BeOfType(typeof(OkObjectResult));
        }

        [Theory]
        [InlineData("truyenfull.vn", "ngao-the-dan-than")]
        public async Task NovelsController_GetDetail_ReturnsOK(string server, string novelSlug)
        {
            //Arrange
            var novelDetail = A.Fake<NovelDetail>();
            A.CallTo(() => novelCrawlerContext.GetNovelDetail(novelSlug)).Returns(novelDetail);
            var controller = new NovelsController(logger, novelCrawlerContext, novelDownloaderContext);

            //Act
            var response = await controller.GetDetail(server, novelSlug);

            //Assert
            response.Should().NotBeNull();
            response.Should().BeOfType(typeof(OkObjectResult));
        }

        [Theory]
        [InlineData("truyenfull.vn", "ngao-the-dan-than", 1, 20)]
        public async Task NovelsController_GetChapters_ReturnsOK(string server, string novelSlug, int page, int perPage)
        {
            //Arrange
            var novelChaptersResult = A.Fake<NovelChaptersResult>();
            A.CallTo(() => novelCrawlerContext.GetChapterList(novelSlug, page, perPage)).Returns(novelChaptersResult);
            var controller = new NovelsController(logger, novelCrawlerContext, novelDownloaderContext);

            //Act
            var response = await controller.GetChapters(server, novelSlug, page, perPage);

            //Assert
            response.Should().NotBeNull();
            response.Should().BeOfType(typeof(OkObjectResult));
        }

        [Theory]
        [InlineData("truyenfull.vn", "ngao-the-dan-than", "chuong-1")]
        public async Task NovelsController_GetContent_ReturnsOK(string server, string novelSlug, string chapterSlug)
        {
            //Arrange
            var novelContent = A.Fake<NovelContent>();
            A.CallTo(() => novelCrawlerContext.GetNovelContent(novelSlug, chapterSlug)).Returns(novelContent);
            var controller = new NovelsController(logger, novelCrawlerContext, novelDownloaderContext);

            //Act
            var response = await controller.GetContent(server, novelSlug, chapterSlug);

            //Assert
            response.Should().NotBeNull();
            response.Should().BeOfType(typeof(OkObjectResult));
        }

        [Fact]
        public void NovelsController_GetDownloadFileExtensions_ReturnsOK()
        {
            //Arrange
            var fileExtensions = A.Fake<IEnumerable<string>>();
            A.CallTo(() => novelDownloaderContext.GetFileExtensions()).Returns(fileExtensions);
            var controller = new NovelsController(logger, novelCrawlerContext, novelDownloaderContext);

            //Act
            var response = controller.GetDownloadFileExtensions();

            //Assert
            response.Should().NotBeNull();
            response.Should().BeOfType(typeof(OkObjectResult));
        }

        [Fact]
        public void NovelsController_DownloadNovel_ReturnsOK()
        {
            //Arrange
            var novelDownloadRequest = A.Fake<NovelDownloadRequest>();
            var novelFileStream = A.Fake<Stream>();
            var novelContent = A.Fake<NovelContent>();
            var novelDetail = A.Fake<NovelDetail>();
            var novelDownloadObject = A.Fake<NovelDownloadObject>();

            A.CallTo(() => novelCrawlerContext.GetNovelDetail(novelDownloadRequest.NovelSlug)).Returns(novelDetail);
            A.CallTo(() => novelDownloaderContext.GetFileStream(novelDownloadObject)).Returns(novelFileStream);
            var controller = new NovelsController(logger, novelCrawlerContext, novelDownloaderContext);

            //Act
            var response = controller.GetDownloadFileExtensions();

            //Assert
            response.Should().NotBeNull();
            response.Should().BeOfType(typeof(OkObjectResult));
        }
    }
}
