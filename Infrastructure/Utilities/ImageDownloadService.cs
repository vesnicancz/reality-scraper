using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RealityScraper.Application.Interfaces.Scraping;
using RealityScraper.Domain.Entities.Realty;
using RealityScraper.Infrastructure.Configuration;

namespace RealityScraper.Infrastructure.Utilities;

public class ImageDownloadService : IImageDownloadService
{
	// URL obrázku pochází ze scrapované (cizí) stránky - limity brání SSRF a zahlcení paměti
	private const long MaxImageSizeBytes = 10 * 1024 * 1024;
	private static readonly TimeSpan DownloadTimeout = TimeSpan.FromSeconds(30);

	// HttpClient sám žádný User-Agent neposílá a část CDN takový požadavek odmítne (typicky 403).
	// Použije se stejná identita, jakou při scrapu vidí portál - obrázky se stahují z týchž serverů,
	// takže rozejít ta dvě nastavení by znamenalo tichý výpadek stahování.
	private const string FallbackUserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/116.0.0.0 Safari/537.36";

	private readonly IHttpClientFactory httpClientFactory;
	private readonly ListingImagePathResolver pathResolver;
	private readonly IUrlSafetyValidator urlSafetyValidator;
	private readonly string userAgent;
	private readonly ILogger<ImageDownloadService> logger;

	public ImageDownloadService(
		IHttpClientFactory httpClientFactory,
		ListingImagePathResolver pathResolver,
		IUrlSafetyValidator urlSafetyValidator,
		IOptions<SeleniumOptions> seleniumOptions,
		ILogger<ImageDownloadService> logger
		)
	{
		this.httpClientFactory = httpClientFactory;
		this.pathResolver = pathResolver;
		this.urlSafetyValidator = urlSafetyValidator;
		this.logger = logger;

		var configuredUserAgent = seleniumOptions.Value.UserAgent;
		userAgent = string.IsNullOrWhiteSpace(configuredUserAgent) ? FallbackUserAgent : configuredUserAgent;
	}

	public async Task DownloadImageAsync(Listing listing, CancellationToken cancellationToken)
	{
		if (string.IsNullOrEmpty(listing.ImageUrl))
		{
			return;
		}

		if (!Uri.TryCreate(listing.ImageUrl, UriKind.Absolute, out var imageUri))
		{
			logger.LogWarning("Neplatná URL obrázku pro inzerát {ListingId}: {ImageUrl}", listing.Id, listing.ImageUrl);
			return;
		}

		if (!await urlSafetyValidator.IsPublicHttpTargetAsync(imageUri, cancellationToken))
		{
			logger.LogWarning("URL obrázku inzerátu {ListingId} míří na nepovolený cíl, stahování se přeskakuje: {ImageUrl}", listing.Id, listing.ImageUrl);
			return;
		}

		logger.LogTrace("Stahuji obrázek pro inzerát {ListingId} z {ImageUrl}", listing.Id, listing.ImageUrl);

		using var httpClient = httpClientFactory.CreateClient();
		httpClient.Timeout = DownloadTimeout;
		httpClient.MaxResponseContentBufferSize = MaxImageSizeBytes;

		// TryAddWithoutValidation, aby překlep v konfiguraci neshodil stahování výjimkou.
		httpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgent);
		byte[] imageBytes;

		using (var response = await httpClient.GetAsync(imageUri, cancellationToken))
		{
			response.EnsureSuccessStatusCode();

			var mediaType = response.Content.Headers.ContentType?.MediaType;
			if (mediaType == null || !mediaType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
			{
				logger.LogWarning("Odpověď pro obrázek inzerátu {ListingId} má neočekávaný Content-Type '{MediaType}', ukládání se přeskakuje.", listing.Id, mediaType);
				return;
			}

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
