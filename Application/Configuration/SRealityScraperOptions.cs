namespace RealityScraper.Application.Configuration;

public class SRealityScraperOptions : BaseScraperOptions
{
	public required string CpmDialogContainerSelector { get; set; }

	public required string CpmAgreeButtonsSelector { get; set; }

	public required string PremiumWindowSelector { get; set; }

	public override bool HasRequiredSelectors()
	{
		return base.HasRequiredSelectors()
			&& !string.IsNullOrWhiteSpace(CpmDialogContainerSelector)
			&& !string.IsNullOrWhiteSpace(CpmAgreeButtonsSelector)
			&& !string.IsNullOrWhiteSpace(PremiumWindowSelector);
	}
}
