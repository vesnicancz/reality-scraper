using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RealityScraper.Application.Interfaces.Scraping;
using RealityScraper.Domain.Entities.Realty;
using RealityScraper.Infrastructure.Configuration;
using RealityScraper.Infrastructure.Utilities;

namespace RealityScraper.Infrastructure.Tests.Utilities;

/// <summary>
/// Stahování jede proti fake handleru, ukládání proti skutečnému disku v dočasném adresáři.
/// </summary>
public sealed class ImageDownloadServiceTests : IDisposable
{
	private readonly string root = Path.Combine(Path.GetTempPath(), $"reality-scraper-tests-{Guid.NewGuid()}");
	private readonly ListingImagePathResolver pathResolver;
	private readonly CapturingHandler handler = new();

	public ImageDownloadServiceTests()
	{
		var configuration = new ConfigurationBuilder()
			.AddInMemoryCollection(new Dictionary<string, string?> { ["FileStorage:ImagePath"] = root })
			.Build();

		pathResolver = new ListingImagePathResolver(configuration);
	}

	public void Dispose()
	{
		handler.Dispose();

		if (Directory.Exists(root))
		{
			Directory.Delete(root, recursive: true);
		}
	}

	private ImageDownloadService CreateSut(string? configuredUserAgent)
	{
		return new ImageDownloadService(
			new StubHttpClientFactory(handler),
			pathResolver,
			new AlwaysAllowedUrlSafetyValidator(),
			Options.Create(new SeleniumOptions { UserAgent = configuredUserAgent }),
			NullLogger<ImageDownloadService>.Instance);
	}

	private static Listing CreateListing()
	{
		return new Listing
		{
			Id = Guid.NewGuid(),
			ExternalId = "ext-1",
			Title = "Prodej domu",
			Location = "Brno",
			Url = "https://example.com/ext-1",
			ImageUrl = "https://cdn.example.com/photos/first.jpg"
		};
	}

	[Fact]
	public async Task DownloadImageAsync_SendsConfiguredUserAgent()
	{
		// Arrange - bez User-Agenta část CDN odpoví 403 a obrázek se nikdy nestáhne
		var sut = CreateSut("Mozilla/5.0 (Test) Chrome/999.0.0.0");

		// Act
		await sut.DownloadImageAsync(CreateListing(), CancellationToken.None);

		// Assert
		Assert.Equal("Mozilla/5.0 (Test) Chrome/999.0.0.0", handler.LastUserAgent);
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("   ")]
	public async Task DownloadImageAsync_FallsBackToDefaultUserAgent_WhenConfigurationIsEmpty(string? configuredUserAgent)
	{
		// Arrange
		var sut = CreateSut(configuredUserAgent);

		// Act
		await sut.DownloadImageAsync(CreateListing(), CancellationToken.None);

		// Assert - hlavička musí odejít i s prázdnou konfigurací, jinak je to tichý výpadek
		Assert.StartsWith("Mozilla/5.0", handler.LastUserAgent);
	}

	[Fact]
	public async Task DownloadImageAsync_SavesImageToResolvedPath()
	{
		// Arrange
		var listing = CreateListing();
		var sut = CreateSut("Mozilla/5.0 (Test) Chrome/999.0.0.0");

		// Act
		await sut.DownloadImageAsync(listing, CancellationToken.None);

		// Assert
		Assert.Equal(CapturingHandler.ImageBytes, await File.ReadAllBytesAsync(pathResolver.GetImageFilePath(listing.Id), CancellationToken.None));
	}

	private sealed class CapturingHandler : HttpMessageHandler
	{
		public static readonly byte[] ImageBytes = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10];

		public string? LastUserAgent { get; private set; }

		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			LastUserAgent = request.Headers.TryGetValues("User-Agent", out var values) ? string.Join(" ", values) : null;

			var response = new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new ByteArrayContent(ImageBytes)
			};
			response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");

			return Task.FromResult(response);
		}
	}

	private sealed class StubHttpClientFactory : IHttpClientFactory
	{
		private readonly HttpMessageHandler handler;

		public StubHttpClientFactory(HttpMessageHandler handler)
		{
			this.handler = handler;
		}

		// disposeHandler: false - handler přežívá jednotlivé klienty a test si ho uklidí sám.
		public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
	}

	private sealed class AlwaysAllowedUrlSafetyValidator : IUrlSafetyValidator
	{
		public Task<bool> IsPublicHttpTargetAsync(Uri uri, CancellationToken cancellationToken) => Task.FromResult(true);
	}
}
