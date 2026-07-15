namespace RealityScraper.Application.Features.Reporting.Model;

public class RemovedListingItem
{
	public Guid ListingId { get; init; }

	public string Title { get; init; } = string.Empty;

	public string Location { get; init; } = string.Empty;

	public decimal? Price { get; init; }

	public string Url { get; init; } = string.Empty;

	public DateTimeOffset CreatedAt { get; init; }

	public DateTimeOffset? RemovedAt { get; init; }

	/// <summary>
	/// True, pokud je k e-mailu přiložen nakešovaný obrázek (cid = ListingId).
	/// </summary>
	public bool HasImage { get; set; }
}
