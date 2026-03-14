namespace RealityScraper.Application.Configuration;

public class SRealityScraperOptions : BaseScraperOptions
{
	public required string CpmDialogContainerSelector { get; set; }

	public required string CpmAgreeButtonsSelector { get; set; }

	public required string PremiumWindowSelector { get; set; }
}