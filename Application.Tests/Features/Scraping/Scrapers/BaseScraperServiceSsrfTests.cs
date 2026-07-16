using Microsoft.Extensions.Logging;
using Moq;
using RealityScraper.Application.Configuration;
using RealityScraper.Application.Features.Scraping.Configuration;
using RealityScraper.Application.Features.Scraping.Scrapers;
using RealityScraper.Application.Interfaces.Scraping;
using RealityScraper.Domain.Enums;

namespace RealityScraper.Application.Tests.Features.Scraping.Scrapers;

public class BaseScraperServiceSsrfTests
{
	private readonly Mock<IWebDriverFactory> webDriverFactoryMock = new();
	private readonly Mock<IUrlSafetyValidator> urlSafetyValidatorMock = new();

	[Fact]
	public async Task ScrapeListingsAsync_DisallowedTarget_DoesNotCreateDriverAndFails()
	{
		// arrange
		urlSafetyValidatorMock
			.Setup(x => x.IsPublicHttpTargetAsync(It.IsAny<Uri>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(false);

		var sut = CreateSut();
		var config = new ScraperConfiguration { Url = "http://169.254.169.254/latest/meta-data" };

		// act
		var result = await sut.ScrapeListingsAsync(config, CancellationToken.None);

		// assert
		Assert.False(result.Success);
		Assert.Empty(result.Listings);
		webDriverFactoryMock.Verify(x => x.CreateDriver(), Times.Never);
	}

	[Fact]
	public async Task ScrapeListingsAsync_MalformedUrl_DoesNotCreateDriverAndFails()
	{
		// arrange - neplatná absolutní URL se ani nedostane k validátoru
		var sut = CreateSut();
		var config = new ScraperConfiguration { Url = "not-a-valid-url" };

		// act
		var result = await sut.ScrapeListingsAsync(config, CancellationToken.None);

		// assert
		Assert.False(result.Success);
		webDriverFactoryMock.Verify(x => x.CreateDriver(), Times.Never);
	}

	private TestScraper CreateSut()
	{
		return new TestScraper(
			Mock.Of<ILogger>(),
			webDriverFactoryMock.Object,
			urlSafetyValidatorMock.Object);
	}

	// Minimální konkrétní scraper pro test guardu v bázové třídě.
	private sealed class TestScraper : BaseScraperService
	{
		public TestScraper(ILogger logger, IWebDriverFactory webDriverFactory, IUrlSafetyValidator urlSafetyValidator)
			: base(logger, webDriverFactory, urlSafetyValidator)
		{
		}

		public override string SiteName => "Test";

		public override ScrapersEnum ScrapersEnum => ScrapersEnum.SReality;

		protected override BaseScraperOptions Options => new()
		{
			ListingSelector = "li",
			DetailLinkSelector = "a",
			TitleSelector = "h2",
			PriceSelector = "p",
			LocationSelector = "span",
			ImageSelector = "img",
			NextPageSelector = ".next"
		};

		protected override string ImageAttribute => "src";

		protected override Task<bool> NavigateToNextPageAsync(
			IWebDriver driver, string baseUrl, int currentPage,
			IReadOnlyList<IWebDriverElement> nextButtons, CancellationToken cancellationToken)
		{
			return Task.FromResult(false);
		}
	}
}
