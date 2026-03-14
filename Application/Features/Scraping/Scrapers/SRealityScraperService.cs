using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RealityScraper.Application.Configuration;
using RealityScraper.Application.Interfaces.Scraping;
using RealityScraper.Domain.Enums;

namespace RealityScraper.Application.Features.Scraping.Scrapers;

public class SRealityScraperService : BaseScraperService
{
	private readonly SRealityScraperOptions options;

	public override string SiteName => "SReality";

	public override ScrapersEnum ScrapersEnum => ScrapersEnum.SReality;

	protected override BaseScraperOptions Options => options;

	protected override string ImageAttribute => "src";

	public SRealityScraperService(
		ILogger<SRealityScraperService> logger,
		IOptions<SRealityScraperOptions> options,
		IWebDriverFactory webDriverFactory)
		: base(logger, webDriverFactory)
	{
		this.options = options.Value;
	}

	protected override bool ValidateExternalId(string? externalId)
	{
		return !string.IsNullOrEmpty(externalId) && long.TryParse(externalId, out _);
	}

	protected override async Task OnPreScrapingAsync(IWebDriver driver, CancellationToken cancellationToken)
	{
		var shadowHost = await driver.FindElementsAsync(options.CpmDialogContainerSelector, cancellationToken);
		if (shadowHost.Count > 0)
		{
			var shadowRoot = await shadowHost.First().GetShadowRootAsync(cancellationToken);
			var agreeButtons = await shadowRoot.FindElementsAsync(options.CpmAgreeButtonsSelector, cancellationToken);
			await agreeButtons.First().ClickAsync(cancellationToken);
			await Task.Delay(5000, cancellationToken);
		}
	}

	protected override async Task<bool> NavigateToNextPageAsync(IWebDriver driver, string baseUrl, int currentPage, IReadOnlyList<IWebDriverElement> nextButtons, CancellationToken cancellationToken)
	{
		var premiumDialog = await driver.FindElementsAsync(options.PremiumWindowSelector, cancellationToken);
		if (premiumDialog.Count > 0)
		{
			await driver.NavigateToUrlAsync(baseUrl + "&strana=" + currentPage, cancellationToken);
		}
		else
		{
			await nextButtons.First().ClickAsync(cancellationToken);
		}
		await Task.Delay(5000, cancellationToken);
		return true;
	}
}