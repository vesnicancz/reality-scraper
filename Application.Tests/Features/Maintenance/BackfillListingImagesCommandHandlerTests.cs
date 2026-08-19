using Moq;
using RealityScraper.Application.Features.Maintenance.BackfillListingImages;
using RealityScraper.Application.Interfaces.Repositories.Realty;
using RealityScraper.Application.Interfaces.Scraping;
using RealityScraper.Domain.Entities.Realty;

namespace RealityScraper.Application.Tests.Features.Maintenance;

public class BackfillListingImagesCommandHandlerTests
{
	// Musí odpovídat MaxDownloadsPerRun v handleru.
	private const int MaxDownloadsPerRun = 200;

	private static readonly DateTimeOffset Now = new(2026, 7, 15, 12, 0, 0, TimeSpan.Zero);

	private readonly Mock<IListingRepository> listingRepositoryMock = new();
	private readonly Mock<IListingImageReader> imageReaderMock = new();
	private readonly Mock<IImageDownloadService> imageDownloadServiceMock = new();

	private BackfillListingImagesCommandHandler CreateSut()
	{
		return new BackfillListingImagesCommandHandler(
			listingRepositoryMock.Object,
			imageReaderMock.Object,
			imageDownloadServiceMock.Object);
	}

	private static Listing CreateListing(string externalId = "ext-1")
	{
		return new Listing
		{
			Id = Guid.NewGuid(),
			ExternalId = externalId,
			Title = "Prodej domu",
			Location = "Brno",
			Url = "https://example.com/1",
			ImageUrl = "https://example.com/1.jpg",
			CreatedAt = Now,
			LastSeenAt = Now,
			PriceFrom = Now
		};
	}

	private void SetupListings(params Listing[] listings)
	{
		listingRepositoryMock
			.Setup(x => x.GetActiveWithImageUrlAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(listings.ToList());
	}

	/// <summary>
	/// Nasimuluje stažení obrázku: po zavolání DownloadImageAsync začne ImageExists vracet true.
	/// </summary>
	private void SetupSuccessfulDownloads(IEnumerable<Guid>? alreadyCached = null)
	{
		var stored = alreadyCached?.ToHashSet() ?? new HashSet<Guid>();

		imageReaderMock
			.Setup(x => x.ImageExists(It.IsAny<Guid>()))
			.Returns((Guid id) => stored.Contains(id));

		imageDownloadServiceMock
			.Setup(x => x.DownloadImageAsync(It.IsAny<Listing>(), It.IsAny<CancellationToken>()))
			.Callback((Listing listing, CancellationToken _) => stored.Add(listing.Id))
			.Returns(Task.CompletedTask);
	}

	[Fact]
	public async Task Handle_ReturnsZeroCounts_WhenThereAreNoListings()
	{
		// Arrange
		SetupListings();
		var sut = CreateSut();

		// Act
		var result = await sut.Handle(new BackfillListingImagesCommand(), CancellationToken.None);

		// Assert
		Assert.True(result.IsSuccess);
		Assert.Equal(new BackfillListingImagesResult(0, 0, 0, 0), result.Value);
		imageDownloadServiceMock.Verify(
			x => x.DownloadImageAsync(It.IsAny<Listing>(), It.IsAny<CancellationToken>()),
			Times.Never);
	}

	[Fact]
	public async Task Handle_SkipsListingsThatAlreadyHaveImage()
	{
		// Arrange
		var withImage = CreateListing("ext-1");
		var withoutImage = CreateListing("ext-2");
		SetupListings(withImage, withoutImage);
		SetupSuccessfulDownloads([withImage.Id]);
		var sut = CreateSut();

		// Act
		var result = await sut.Handle(new BackfillListingImagesCommand(), CancellationToken.None);

		// Assert
		Assert.True(result.IsSuccess);
		Assert.Equal(2, result.Value.CheckedCount);
		Assert.Equal(1, result.Value.DownloadedCount);
		Assert.Equal(0, result.Value.FailedCount);
		imageDownloadServiceMock.Verify(x => x.DownloadImageAsync(withImage, It.IsAny<CancellationToken>()), Times.Never);
		imageDownloadServiceMock.Verify(x => x.DownloadImageAsync(withoutImage, It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task Handle_CountsAsFailed_WhenDownloadSilentlySkipsTheImage()
	{
		// Arrange
		// DownloadImageAsync vrací void i když nic neuloží (neplatná URL, nepovolený cíl,
		// odpověď bez Content-Type image/*) - handler to pozná jen podle chybějícího souboru.
		SetupListings(CreateListing());
		imageReaderMock.Setup(x => x.ImageExists(It.IsAny<Guid>())).Returns(false);
		imageDownloadServiceMock
			.Setup(x => x.DownloadImageAsync(It.IsAny<Listing>(), It.IsAny<CancellationToken>()))
			.Returns(Task.CompletedTask);
		var sut = CreateSut();

		// Act
		var result = await sut.Handle(new BackfillListingImagesCommand(), CancellationToken.None);

		// Assert
		Assert.True(result.IsSuccess);
		Assert.Equal(1, result.Value.CheckedCount);
		Assert.Equal(0, result.Value.DownloadedCount);
		Assert.Equal(1, result.Value.FailedCount);
		Assert.Equal(0, result.Value.RemainingCount);
	}

	[Fact]
	public async Task Handle_CountsAsFailedAndContinues_WhenDownloadThrows()
	{
		// Arrange
		var failing = CreateListing("ext-1");
		var succeeding = CreateListing("ext-2");
		SetupListings(failing, succeeding);
		SetupSuccessfulDownloads();
		imageDownloadServiceMock
			.Setup(x => x.DownloadImageAsync(failing, It.IsAny<CancellationToken>()))
			.ThrowsAsync(new HttpRequestException("boom"));
		var sut = CreateSut();

		// Act
		var result = await sut.Handle(new BackfillListingImagesCommand(), CancellationToken.None);

		// Assert
		Assert.True(result.IsSuccess);
		Assert.Equal(2, result.Value.CheckedCount);
		Assert.Equal(1, result.Value.DownloadedCount);
		Assert.Equal(1, result.Value.FailedCount);
		imageDownloadServiceMock.Verify(x => x.DownloadImageAsync(succeeding, It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task Handle_PropagatesCancellation()
	{
		// Arrange
		SetupListings(CreateListing(), CreateListing("ext-2"));
		imageReaderMock.Setup(x => x.ImageExists(It.IsAny<Guid>())).Returns(false);
		imageDownloadServiceMock
			.Setup(x => x.DownloadImageAsync(It.IsAny<Listing>(), It.IsAny<CancellationToken>()))
			.ThrowsAsync(new OperationCanceledException());
		var sut = CreateSut();

		// Act & Assert
		await Assert.ThrowsAsync<OperationCanceledException>(
			() => sut.Handle(new BackfillListingImagesCommand(), CancellationToken.None));
	}

	[Fact]
	public async Task Handle_StopsAtMaxDownloadsPerRun_AndReportsRemaining()
	{
		// Arrange
		var listings = Enumerable.Range(0, MaxDownloadsPerRun + 5)
			.Select(i => CreateListing($"ext-{i}"))
			.ToArray();
		SetupListings(listings);
		SetupSuccessfulDownloads();
		var sut = CreateSut();

		// Act
		var result = await sut.Handle(new BackfillListingImagesCommand(), CancellationToken.None);

		// Assert
		Assert.True(result.IsSuccess);
		Assert.Equal(MaxDownloadsPerRun + 5, result.Value.CheckedCount);
		Assert.Equal(MaxDownloadsPerRun, result.Value.DownloadedCount);
		Assert.Equal(0, result.Value.FailedCount);
		Assert.Equal(5, result.Value.RemainingCount);
		imageDownloadServiceMock.Verify(
			x => x.DownloadImageAsync(It.IsAny<Listing>(), It.IsAny<CancellationToken>()),
			Times.Exactly(MaxDownloadsPerRun));
	}

	[Fact]
	public async Task Handle_DoesNotCountAlreadyCachedListingsTowardsTheLimit()
	{
		// Arrange
		// Inzeráty s hotovým obrázkem se přeskakují dřív, než se sáhne na limit,
		// takže jich může projít libovolně mnoho.
		var cached = Enumerable.Range(0, MaxDownloadsPerRun + 50)
			.Select(i => CreateListing($"cached-{i}"))
			.ToArray();
		var missing = CreateListing("missing");
		SetupListings([.. cached, missing]);
		SetupSuccessfulDownloads(cached.Select(l => l.Id));
		var sut = CreateSut();

		// Act
		var result = await sut.Handle(new BackfillListingImagesCommand(), CancellationToken.None);

		// Assert
		Assert.Equal(cached.Length + 1, result.Value.CheckedCount);
		Assert.Equal(1, result.Value.DownloadedCount);
		Assert.Equal(0, result.Value.RemainingCount);
	}
}
