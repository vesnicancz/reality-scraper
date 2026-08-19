using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using RealityScraper.Infrastructure.Utilities;

namespace RealityScraper.Infrastructure.Tests.Utilities;

/// <summary>
/// Čtení jede proti skutečnému disku - kontroluje se tím i to, že reader a resolver
/// míří na stejný soubor.
/// </summary>
public sealed class ListingImageReaderTests : IDisposable
{
	private readonly string root = Path.Combine(Path.GetTempPath(), $"reality-scraper-tests-{Guid.NewGuid()}");
	private readonly ListingImagePathResolver pathResolver;
	private readonly ListingImageReader sut;

	public ListingImageReaderTests()
	{
		var configuration = new ConfigurationBuilder()
			.AddInMemoryCollection(new Dictionary<string, string?> { ["FileStorage:ImagePath"] = root })
			.Build();

		pathResolver = new ListingImagePathResolver(configuration);
		sut = new ListingImageReader(pathResolver, NullLogger<ListingImageReader>.Instance);
	}

	public void Dispose()
	{
		if (Directory.Exists(root))
		{
			Directory.Delete(root, recursive: true);
		}
	}

	private void WriteImage(Guid listingId, byte[] content)
	{
		Directory.CreateDirectory(pathResolver.GetImageFolderPath(listingId));
		File.WriteAllBytes(pathResolver.GetImageFilePath(listingId), content);
	}

	[Fact]
	public void ImageExists_ReturnsFalse_WhenImageWasNotDownloaded()
	{
		Assert.False(sut.ImageExists(Guid.NewGuid()));
	}

	[Fact]
	public void ImageExists_ReturnsTrue_WhenImageIsOnDisk()
	{
		// Arrange
		var listingId = Guid.NewGuid();
		WriteImage(listingId, [1, 2, 3]);

		// Act & Assert
		Assert.True(sut.ImageExists(listingId));
	}

	[Fact]
	public async Task TryReadImageAsync_ReturnsNull_WhenImageIsMissing()
	{
		// Act
		var result = await sut.TryReadImageAsync(Guid.NewGuid(), CancellationToken.None);

		// Assert
		Assert.Null(result);
	}

	[Fact]
	public async Task TryReadImageAsync_ReturnsStoredBytes()
	{
		// Arrange
		var listingId = Guid.NewGuid();
		var content = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 };
		WriteImage(listingId, content);

		// Act
		var result = await sut.TryReadImageAsync(listingId, CancellationToken.None);

		// Assert
		Assert.Equal(content, result);
	}

	[Fact]
	public async Task TryReadImageAsync_ReadsOnlyTheRequestedListing()
	{
		// Arrange
		// Sharding podle prvních dvou znaků Id nesmí smíchat inzeráty ve stejné podsložce.
		var first = Guid.NewGuid();
		var second = Guid.NewGuid();
		WriteImage(first, [1]);
		WriteImage(second, [2]);

		// Act & Assert
		Assert.Equal([1], await sut.TryReadImageAsync(first, CancellationToken.None));
		Assert.Equal([2], await sut.TryReadImageAsync(second, CancellationToken.None));
	}
}
