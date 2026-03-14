using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RealityScraper.Application.Configuration;
using RealityScraper.Application.Interfaces.Scraping;
using RealityScraper.Domain.Enums;

namespace RealityScraper.Application.Features.Scraping.Scrapers;

public class RealityIdnesScraperService : BaseScraperService
{
	private readonly RealityIdnesScraperOptions options;

	public override string SiteName => "Reality Idnes";

	public override ScrapersEnum ScrapersEnum => ScrapersEnum.RealityIdnes;

	protected override BaseScraperOptions Options => options;

	protected override string ImageAttribute => "data-src";

	public RealityIdnesScraperService(
		ILogger<RealityIdnesScraperService> logger,
		IOptions<RealityIdnesScraperOptions> options,
		IWebDriverFactory webDriverFactory)
		: base(logger, webDriverFactory)
	{
		this.options = options.Value;
	}

	protected override async Task<bool> NavigateToNextPageAsync(IWebDriver driver, string baseUrl, int currentPage, IReadOnlyList<IWebDriverElement> nextButtons, CancellationToken cancellationToken)
	{
		var nextPageElement = nextButtons.First();
		var nextPageLink = await nextPageElement.GetAttributeAsync("href", cancellationToken);
		await driver.NavigateToUrlAsync(nextPageLink, cancellationToken);
		return true;
	}
}