namespace RealityScraper.Application.Interfaces.Scraping;

public interface IListingImageReader
{
	/// <summary>
	/// Načte nakešovaný obrázek inzerátu z lokálního úložiště.
	/// Vrací null, pokud obrázek není k dispozici.
	/// </summary>
	Task<byte[]?> TryReadImageAsync(Guid listingId, CancellationToken cancellationToken);
}
