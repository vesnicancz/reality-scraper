using Microsoft.Extensions.Logging;
using Moq;
using RealityScraper.Application.Configuration;
using RealityScraper.Application.Features.Scraping.Scrapers;
using RealityScraper.Application.Interfaces.Scraping;
using RealityScraper.Domain.Enums;

namespace RealityScraper.Application.Tests.Features.Scraping.Scrapers;

public class BaseScraperServiceTests
{
	/// <summary>
	/// Odvození ID je chráněné, testuje se tedy přes minimálního potomka. Zbytek scrapovací
	/// smyčky potřebuje prohlížeč, proto se tady neověřuje.
	/// </summary>
	private sealed class TestScraperService : BaseScraperService
	{
		public TestScraperService()
			: base(Mock.Of<ILogger>(), Mock.Of<IWebDriverFactory>(), Mock.Of<IUrlSafetyValidator>())
		{
		}

		public override string SiteName => "Test";

		public override ScrapersEnum ScrapersEnum => ScrapersEnum.SReality;

		protected override string ImageAttribute => "src";

		protected override BaseScraperOptions Options => new BaseScraperOptions
		{
			ListingSelector = "li",
			DetailLinkSelector = "a",
			TitleSelector = "h2",
			PriceSelector = ".price",
			LocationSelector = ".location",
			ImageSelector = "img",
			NextPageSelector = ".next"
		};

		protected override Task<bool> NavigateToNextPageAsync(
			IWebDriver driver, string baseUrl, int currentPage,
			IReadOnlyList<IWebDriverElement> nextButtons, CancellationToken cancellationToken)
		{
			return Task.FromResult(false);
		}

		public string? Extract(string detailUrl) => ExtractExternalId(new Uri(detailUrl));
	}

	[Theory]
	// Reality Idnes vrací odkaz s koncovým lomítkem - dřív zůstávalo součástí ID.
	[InlineData("https://reality.idnes.cz/detail/prodej/dum/vesnice/69df7bee84ad6aa38202ec65/", "69df7bee84ad6aa38202ec65")]
	// SReality vrací odkaz bez lomítka, výsledek se nesmí změnit.
	[InlineData("https://www.sreality.cz/detail/prodej/dum/rodinny/vesnice/4208955468", "4208955468")]
	// Tvar odkazu na ID nesmí mít vliv.
	[InlineData("https://reality.idnes.cz/detail/prodej/dum/vesnice/abc123//", "abc123")]
	[InlineData("https://reality.idnes.cz/detail/prodej/dum/vesnice/abc123/?utm_source=x", "abc123")]
	[InlineData("https://reality.idnes.cz/detail/prodej/dum/vesnice/abc123#fotky", "abc123")]
	public void ExtractExternalId_ReturnsLastPathSegmentWithoutSlashes(string detailUrl, string expected)
	{
		var sut = new TestScraperService();

		var result = sut.Extract(detailUrl);

		Assert.Equal(expected, result);
	}

	[Theory]
	[InlineData("https://reality.idnes.cz/")]
	[InlineData("https://reality.idnes.cz")]
	public void ExtractExternalId_UrlWithoutPath_ReturnsNull(string detailUrl)
	{
		var sut = new TestScraperService();

		var result = sut.Extract(detailUrl);

		Assert.Null(result);
	}
}
