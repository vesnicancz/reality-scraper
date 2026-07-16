using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using RealityScraper.Application.Interfaces.Scraping;
using RealityScraper.Domain.Entities.Realty;

namespace RealityScraper.Infrastructure.Utilities;

public class ImageDownloadService : IImageDownloadService
{
	// URL obrázku pochází ze scrapované (cizí) stránky - limity brání SSRF a zahlcení paměti
	private const long MaxImageSizeBytes = 10 * 1024 * 1024;
	private static readonly TimeSpan DownloadTimeout = TimeSpan.FromSeconds(30);

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

		if (!Uri.TryCreate(listing.ImageUrl, UriKind.Absolute, out var imageUri))
		{
			logger.LogWarning("Neplatná URL obrázku pro inzerát {ListingId}: {ImageUrl}", listing.Id, listing.ImageUrl);
			return;
		}

		if (!await IsAllowedTargetAsync(imageUri, cancellationToken))
		{
			logger.LogWarning("URL obrázku inzerátu {ListingId} míří na nepovolený cíl, stahování se přeskakuje: {ImageUrl}", listing.Id, listing.ImageUrl);
			return;
		}

		logger.LogTrace("Stahuji obrázek pro inzerát {ListingId} z {ImageUrl}", listing.Id, listing.ImageUrl);

		using var httpClient = httpClientFactory.CreateClient();
		httpClient.Timeout = DownloadTimeout;
		httpClient.MaxResponseContentBufferSize = MaxImageSizeBytes;
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

	/// <summary>
	/// Povoluje jen http/https cíle resolvované na veřejné adresy - brání stahování
	/// z interní sítě (SSRF) přes podvržený atribut obrázku na scrapované stránce.
	/// </summary>
	private static async Task<bool> IsAllowedTargetAsync(Uri uri, CancellationToken cancellationToken)
	{
		if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
		{
			return false;
		}

		IPAddress[] addresses;
		try
		{
			addresses = await Dns.GetHostAddressesAsync(uri.DnsSafeHost, cancellationToken);
		}
		catch (SocketException)
		{
			return false;
		}

		return addresses.Length > 0 && addresses.All(IsPublicAddress);
	}

	private static bool IsPublicAddress(IPAddress address)
	{
		if (address.IsIPv4MappedToIPv6)
		{
			address = address.MapToIPv4();
		}

		if (IPAddress.IsLoopback(address) || address.IsIPv6LinkLocal || address.IsIPv6UniqueLocal || address.IsIPv6Multicast)
		{
			return false;
		}

		if (address.AddressFamily == AddressFamily.InterNetwork)
		{
			var bytes = address.GetAddressBytes();
			return bytes[0] switch
			{
				0 => false,
				10 => false,
				100 when bytes[1] >= 64 && bytes[1] <= 127 => false, // CGNAT 100.64.0.0/10
				127 => false,
				169 when bytes[1] == 254 => false, // link-local vč. cloud metadata 169.254.169.254
				172 when bytes[1] >= 16 && bytes[1] <= 31 => false,
				192 when bytes[1] == 168 => false,
				_ => true
			};
		}

		return true;
	}
}
