using Microsoft.Extensions.Logging;
using Moq;
using RealityScraper.Application.Features.Scraping;
using RealityScraper.Application.Features.Scraping.Model.Report;
using RealityScraper.Application.Interfaces.Repositories.Realty;
using RealityScraper.Domain.Entities.Realty;
using RealityScraper.SharedKernel;

namespace RealityScraper.Application.Tests.Features.Scraping;

public class ListingChangeProcessorTests
{
	private static readonly DateTimeOffset Now = new(2026, 7, 15, 12, 0, 0, TimeSpan.Zero);
	private static readonly DateTimeOffset Earlier = Now.AddDays(-10);
	private static readonly Guid TaskId = Guid.NewGuid();

	private readonly Mock<IListingRepository> listingRepositoryMock = new();
	private readonly Mock<IDateTimeProvider> dateTimeProviderMock = new();

	private ListingChangeProcessor CreateSut()
	{
		dateTimeProviderMock.Setup(x => x.UtcNow).Returns(Now);
		return new ListingChangeProcessor(
			listingRepositoryMock.Object,
			dateTimeProviderMock.Object,
			Mock.Of<ILogger<ListingChangeProcessor>>());
	}

	private static ListingItem NewItem(string externalId, decimal? price = 5_000_000)
	{
		return new ListingItem
		{
			Title = $"Prodej domu {externalId}",
			Price = price,
			Location = "Brno",
			Url = $"https://example.com/{externalId}",
			ImageUrl = $"https://example.com/{externalId}.jpg",
			ExternalId = externalId
		};
	}

	private static ListingItemWithNewPrice PriceChangedItem(string externalId, decimal? newPrice, decimal? oldPrice)
	{
		return new ListingItemWithNewPrice
		{
			Title = $"Prodej domu {externalId}",
			Price = newPrice,
			OldPrice = oldPrice,
			Location = "Brno",
			Url = $"https://example.com/{externalId}",
			ImageUrl = $"https://example.com/{externalId}.jpg",
			ExternalId = externalId
		};
	}

	private static Listing ExistingListing(string externalId, decimal? price)
	{
		return new Listing
		{
			Id = Guid.NewGuid(),
			ExternalId = externalId,
			Title = "Prodej domu",
			Location = "Brno",
			Url = $"https://example.com/{externalId}",
			ImageUrl = string.Empty,
			Price = price,
			CreatedAt = Earlier,
			LastSeenAt = Earlier,
			PriceFrom = Earlier,
			ScraperTaskId = TaskId
		};
	}

	private static ScrapingReport CreateReport(params PortalReport[] results)
	{
		return new ScrapingReport
		{
			ScraperTaskId = TaskId,
			TaskName = "task",
			ReportDate = Now,
			ScrapingSucceeded = true,
			Results = results.ToList()
		};
	}

	private static PortalReport Portal(
		string siteName = "sreality",
		List<ListingItem>? newListings = null,
		List<ListingItemWithNewPrice>? priceChanged = null)
	{
		return new PortalReport
		{
			SiteName = siteName,
			NewListings = newListings ?? [],
			PriceChangedListings = priceChanged ?? []
		};
	}

	[Fact]
	public async Task ProcessChangesAsync_AddsNewListingsToRepositoryAndReturnsThemForDownload()
	{
		// Arrange
		var report = CreateReport(Portal(newListings: [NewItem("ext-1", 4_200_000)]));
		var added = new List<Listing>();
		listingRepositoryMock.Setup(x => x.Add(It.IsAny<Listing>())).Callback((Listing l) => added.Add(l));
		var sut = CreateSut();

		// Act
		var toDownload = await sut.ProcessChangesAsync(report, CancellationToken.None);

		// Assert
		var listing = Assert.Single(added);
		Assert.Equal("ext-1", listing.ExternalId);
		Assert.Equal("Prodej domu ext-1", listing.Title);
		Assert.Equal(4_200_000, listing.Price);
		Assert.Equal("Brno", listing.Location);
		Assert.Equal("https://example.com/ext-1", listing.Url);
		Assert.Equal("https://example.com/ext-1.jpg", listing.ImageUrl);
		Assert.Equal(TaskId, listing.ScraperTaskId);
		Assert.Equal(Now, listing.CreatedAt);
		Assert.Equal(Now, listing.LastSeenAt);
		Assert.Equal(Now, listing.PriceFrom);
		Assert.Same(listing, Assert.Single(toDownload));
	}

	[Fact]
	public async Task ProcessChangesAsync_AddsNewListingsFromAllPortals()
	{
		// Arrange
		var report = CreateReport(
			Portal("sreality", newListings: [NewItem("s-1"), NewItem("s-2")]),
			Portal("reality.idnes", newListings: [NewItem("i-1")]));
		var sut = CreateSut();

		// Act
		var toDownload = await sut.ProcessChangesAsync(report, CancellationToken.None);

		// Assert
		Assert.Equal(["s-1", "s-2", "i-1"], toDownload.Select(l => l.ExternalId));
		listingRepositoryMock.Verify(x => x.Add(It.IsAny<Listing>()), Times.Exactly(3));
	}

	[Fact]
	public async Task ProcessChangesAsync_DoesNotLoadExistingListings_WhenThereAreNoPriceChanges()
	{
		// Arrange
		// Dotaz do DB má smysl jen kvůli cenovým změnám - u samotných nových inzerátů je zbytečný.
		var report = CreateReport(Portal(newListings: [NewItem("ext-1")]));
		var sut = CreateSut();

		// Act
		await sut.ProcessChangesAsync(report, CancellationToken.None);

		// Assert
		listingRepositoryMock.Verify(
			x => x.GetByScraperTaskIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
			Times.Never);
	}

	[Fact]
	public async Task ProcessChangesAsync_WritesPreviousPriceToHistoryAndUpdatesCurrentPrice()
	{
		// Arrange
		var existing = ExistingListing("ext-1", 5_000_000);
		listingRepositoryMock
			.Setup(x => x.GetByScraperTaskIdAsync(TaskId, It.IsAny<CancellationToken>()))
			.ReturnsAsync([existing]);
		var report = CreateReport(Portal(priceChanged: [PriceChangedItem("ext-1", 4_500_000, 5_000_000)]));
		var sut = CreateSut();

		// Act
		var toDownload = await sut.ProcessChangesAsync(report, CancellationToken.None);

		// Assert
		var history = Assert.Single(existing.PriceHistories);
		Assert.Equal(5_000_000, history.Price);
		Assert.Equal(Earlier, history.RecordedAt);
		Assert.Equal(4_500_000, existing.Price);
		Assert.Equal(Now, existing.PriceFrom);
		Assert.Equal(Now, existing.LastSeenAt);

		// Cenová změna nemění obrázek, takže se nestahuje znovu.
		Assert.Empty(toDownload);
	}

	[Fact]
	public async Task ProcessChangesAsync_SkipsPriceChange_WhenListingIsNotInDatabase()
	{
		// Arrange
		var other = ExistingListing("ext-other", 1_000_000);
		listingRepositoryMock
			.Setup(x => x.GetByScraperTaskIdAsync(TaskId, It.IsAny<CancellationToken>()))
			.ReturnsAsync([other]);
		var report = CreateReport(Portal(priceChanged: [PriceChangedItem("ext-missing", 4_500_000, 5_000_000)]));
		var sut = CreateSut();

		// Act
		var toDownload = await sut.ProcessChangesAsync(report, CancellationToken.None);

		// Assert
		Assert.Empty(toDownload);
		Assert.Empty(other.PriceHistories);
		Assert.Equal(1_000_000, other.Price);
		Assert.Equal(Earlier, other.LastSeenAt);
	}

	[Fact]
	public async Task ProcessChangesAsync_HandlesNewListingsAndPriceChangesTogether()
	{
		// Arrange
		var existing = ExistingListing("ext-old", 3_000_000);
		listingRepositoryMock
			.Setup(x => x.GetByScraperTaskIdAsync(TaskId, It.IsAny<CancellationToken>()))
			.ReturnsAsync([existing]);
		var report = CreateReport(Portal(
			newListings: [NewItem("ext-new")],
			priceChanged: [PriceChangedItem("ext-old", 2_800_000, 3_000_000)]));
		var sut = CreateSut();

		// Act
		var toDownload = await sut.ProcessChangesAsync(report, CancellationToken.None);

		// Assert
		Assert.Equal("ext-new", Assert.Single(toDownload).ExternalId);
		Assert.Equal(2_800_000, existing.Price);
		Assert.Equal(3_000_000, Assert.Single(existing.PriceHistories).Price);
	}

	[Fact]
	public async Task ProcessChangesAsync_KeepsOlderHistoryEntries()
	{
		// Arrange
		var existing = ExistingListing("ext-1", 5_000_000);
		existing.PriceHistories.Add(new PriceHistory { Price = 5_500_000, RecordedAt = Earlier.AddDays(-20) });
		listingRepositoryMock
			.Setup(x => x.GetByScraperTaskIdAsync(TaskId, It.IsAny<CancellationToken>()))
			.ReturnsAsync([existing]);
		var report = CreateReport(Portal(priceChanged: [PriceChangedItem("ext-1", 4_500_000, 5_000_000)]));
		var sut = CreateSut();

		// Act
		await sut.ProcessChangesAsync(report, CancellationToken.None);

		// Assert
		Assert.Equal([5_500_000, 5_000_000], existing.PriceHistories.Select(h => h.Price));
	}

	[Fact]
	public async Task ProcessChangesAsync_ReturnsEmptyList_WhenReportHasNoChanges()
	{
		// Arrange
		var report = CreateReport(Portal());
		var sut = CreateSut();

		// Act
		var toDownload = await sut.ProcessChangesAsync(report, CancellationToken.None);

		// Assert
		Assert.Empty(toDownload);
		listingRepositoryMock.Verify(x => x.Add(It.IsAny<Listing>()), Times.Never);
		listingRepositoryMock.Verify(
			x => x.GetByScraperTaskIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
			Times.Never);
	}
}
