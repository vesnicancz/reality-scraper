using Microsoft.Extensions.Logging;
using RealityScraper.Application.Interfaces.Scraping;

namespace RealityScraper.Infrastructure.Utilities;

public class ListingImageReader : IListingImageReader
{
	private readonly ListingImagePathResolver pathResolver;
	private readonly ILogger<ListingImageReader> logger;

	public ListingImageReader(ListingImagePathResolver pathResolver, ILogger<ListingImageReader> logger)
	{
		this.pathResolver = pathResolver;
		this.logger = logger;
	}

	public async Task<byte[]?> TryReadImageAsync(Guid listingId, CancellationToken cancellationToken)
	{
		var imageFilePath = pathResolver.GetImageFilePath(listingId);

		try
		{
			if (!File.Exists(imageFilePath))
			{
				logger.LogDebug("Nakešovaný obrázek inzerátu {ListingId} nebyl nalezen: {Path}", listingId, imageFilePath);
				return null;
			}

			return await File.ReadAllBytesAsync(imageFilePath, cancellationToken);
		}
		catch (OperationCanceledException)
		{
			throw;
		}
		catch (Exception ex)
		{
			logger.LogWarning(ex, "Nepodařilo se načíst nakešovaný obrázek inzerátu {ListingId} z {Path}.", listingId, imageFilePath);
			return null;
		}
	}
}
