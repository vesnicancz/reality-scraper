using RealityScraper.Model;

namespace RealityScraper.Scraping;

public class ImageDownloadService : IImageDownloadService
{
	private readonly IHttpClientFactory httpClientFactory;

	public ImageDownloadService(IHttpClientFactory httpClientFactory)
	{
		this.httpClientFactory = httpClientFactory;
	}

	public async Task DownloadImageAsync(Listing listing, CancellationToken cancellationToken)
	{
		var imageUri = new Uri(listing.ImageUrl);
		using var httpClient = httpClientFactory.CreateClient();
		httpClient.BaseAddress = imageUri;
		byte[] imageBytes;

		using (var response = await httpClient.GetAsync(imageUri, cancellationToken))
		{
			response.EnsureSuccessStatusCode();
			imageBytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
		}

		var folder = Path.Combine(Directory.GetCurrentDirectory(), "App_Data", "Images", ((listing.Id / 10000) + 1).ToString());
		if (!Directory.Exists(folder))
		{
			Directory.CreateDirectory(folder);
		}

		var imageFilePath = Path.Combine(folder, $"{listing.Id}.jpg");
		await File.WriteAllBytesAsync(imageFilePath, imageBytes, cancellationToken);
	}
}