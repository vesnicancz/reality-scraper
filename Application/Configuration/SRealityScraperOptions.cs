namespace RealityScraper.Application.Configuration;

public class SRealityScraperOptions : BaseScraperOptions
{
	public string CpmDialogContainerSelector { get; set; }

	public string CpmAgreeButtonsSelector { get; set; }

	public string PremiumWindowSelector { get; set; }
}