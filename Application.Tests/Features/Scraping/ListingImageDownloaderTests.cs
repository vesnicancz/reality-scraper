using Microsoft.Extensions.Logging;
using Moq;
using RealityScraper.Application.Features.Scraping;
using RealityScraper.Application.Interfaces.Scraping;
using RealityScraper.Domain.Entities.Realty;

namespace RealityScraper.Application.Tests.Features.Scraping;

public class ListingImageDownloaderTests
{
	private readonly Mock<IImageDownloadService> imageDownloadServiceMock = new();

	private ListingImageDownloader CreateSut()
	{
		return new ListingImageDownloader(
			imageDownloadServiceMock.Object,
			Mock.Of<ILogger<ListingImageDownloader>>());
	}

	private static Listing CreateListing(string externalId)
	{
		return new Listing
		{
			Id = Guid.NewGuid(),
			ExternalId = externalId,
			Title = "Prodej domu",
			Location = "Brno",
			Url = $"https://example.com/{externalId}",
			ImageUrl = $"https://example.com/{externalId}.jpg"
		};
	}

	[Fact]
	public async Task DownloadImagesAsync_DoesNothing_ForEmptyList()
	{
		// Arrange
		var sut = CreateSut();

		// Act
		await sut.DownloadImagesAsync([], CancellationToken.None);

		// Assert
		imageDownloadServiceMock.Verify(
			x => x.DownloadImageAsync(It.IsAny<Listing>(), It.IsAny<CancellationToken>()),
			Times.Never);
	}

	[Fact]
	public async Task DownloadImagesAsync_DownloadsEveryListing()
	{
		// Arrange
		var listings = new List<Listing> { CreateListing("ext-1"), CreateListing("ext-2") };
		var sut = CreateSut();

		// Act
		await sut.DownloadImagesAsync(listings, CancellationToken.None);

		// Assert
		foreach (var listing in listings)
		{
			imageDownloadServiceMock.Verify(x => x.DownloadImageAsync(listing, It.IsAny<CancellationToken>()), Times.Once);
		}
	}

	[Fact]
	public async Task DownloadImagesAsync_ContinuesWithRemainingListings_WhenOneDownloadFails()
	{
		// Arrange
		// Neúspěšné stažení obrázku nesmí shodit celý běh - report je už uložený.
		var failing = CreateListing("ext-1");
		var succeeding = CreateListing("ext-2");
		imageDownloadServiceMock
			.Setup(x => x.DownloadImageAsync(failing, It.IsAny<CancellationToken>()))
			.ThrowsAsync(new HttpRequestException("boom"));
		var sut = CreateSut();

		// Act
		await sut.DownloadImagesAsync([failing, succeeding], CancellationToken.None);

		// Assert
		imageDownloadServiceMock.Verify(x => x.DownloadImageAsync(succeeding, It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task DownloadImagesAsync_PropagatesCancellation()
	{
		// Arrange
		var listings = new List<Listing> { CreateListing("ext-1"), CreateListing("ext-2") };
		imageDownloadServiceMock
			.Setup(x => x.DownloadImageAsync(It.IsAny<Listing>(), It.IsAny<CancellationToken>()))
			.ThrowsAsync(new OperationCanceledException());
		var sut = CreateSut();

		// Act & Assert
		await Assert.ThrowsAsync<OperationCanceledException>(
			() => sut.DownloadImagesAsync(listings, CancellationToken.None));

		imageDownloadServiceMock.Verify(
			x => x.DownloadImageAsync(It.IsAny<Listing>(), It.IsAny<CancellationToken>()),
			Times.Once);
	}
}
