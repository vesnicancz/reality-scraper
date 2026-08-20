namespace RealityScraper.Web.Shared.Models.Maintenance;

public class BackfillListingImagesResult
{
	/// <summary>
	/// Počet prověřených živých inzerátů s vyplněnou URL obrázku.
	/// </summary>
	public int CheckedCount { get; set; }

	public int DownloadedCount { get; set; }

	public int FailedCount { get; set; }

	/// <summary>
	/// Inzeráty, na které se kvůli limitu jednoho běhu nedostalo. Nenulová hodnota
	/// znamená, že je potřeba akci spustit znovu.
	/// </summary>
	public int RemainingCount { get; set; }
}
