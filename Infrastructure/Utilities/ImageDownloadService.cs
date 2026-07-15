using Microsoft.Extensions.Logging;
using RealityScraper.Application.Interfaces.Scraping;
using RealityScraper.Domain.Entities.Realty;

namespace RealityScraper.Infrastructure.Utilities;

public class ImageDownloadService : IImageDownloadService
{
	private readonly IHttpClientFactory httpClientFactory;
	private readonly ListingImagePathResolver pathResolver;
	private readonly ILogger<ImageDownloadService> logger;

	public ImageDownloadService(
		IHttpClientFactory httpClientFactory,
		ListingImagePathResolver pathResolver,
		ILogger<ImageDownloadService> logger
		)
	{
		this.httpClientFactory = httpClientFactory;
		this.pathResolver = pathResolver;
		this.logger = logger;
	}

	public async Task DownloadImageAsync(Listing listing, CancellationToken cancellationToken)
	{
		if (string.IsNullOrEmpty(listing.ImageUrl))
		{
			return;
		}

		logger.LogTrace("Stahuji obrázek pro inzerát {ListingId} z {ImageUrl}", listing.Id, listing.ImageUrl);

		var imageUri = new Uri(listing.ImageUrl);
		using var httpClient = httpClientFactory.CreateClient();
		httpClient.BaseAddress = imageUri;
		byte[] imageBytes;

		using (var response = await httpClient.GetAsync(imageUri, cancellationToken))
		{
			response.EnsureSuccessStatusCode();
			imageBytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
		}

		var folder = pathResolver.GetImageFolderPath(listing.Id);
		if (!Directory.Exists(folder))
		{
			Directory.CreateDirectory(folder);
		}

		var imageFilePath = pathResolver.GetImageFilePath(listing.Id);
		await File.WriteAllBytesAsync(imageFilePath, imageBytes, cancellationToken);
	}
}