using Moq;
using RealityScraper.Application.Features.Dashboard.GetSummary;
using RealityScraper.Application.Interfaces.Repositories.Configuration;
using RealityScraper.Application.Interfaces.Repositories.Realty;
using RealityScraper.Domain.Entities.Realty;
using RealityScraper.Domain.Entities.Tasks;
using RealityScraper.SharedKernel;

namespace RealityScraper.Application.Tests.Features.Dashboard;

public class GetDashboardSummaryQueryHandlerTests
{
	private static readonly DateTimeOffset Now = new(2026, 7, 15, 12, 0, 0, TimeSpan.Zero);

	private readonly Mock<IListingRepository> listingRepositoryMock = new();
	private readonly Mock<IScraperTaskRepository> scraperTaskRepositoryMock = new();
	private readonly Mock<IDateTimeProvider> dateTimeProviderMock = new();

	public GetDashboardSummaryQueryHandlerTests()
	{
		dateTimeProviderMock.SetupGet(x => x.UtcNow).Returns(Now);
		SetupRepositories(new ListingDashboardStats(0, 0, 0, 0), [], []);
	}

	private GetDashboardSummaryQueryHandler CreateSut()
	{
		return new GetDashboardSummaryQueryHandler(
			listingRepositoryMock.Object,
			scraperTaskRepositoryMock.Object,
			dateTimeProviderMock.Object);
	}

	private void SetupRepositories(
		ListingDashboardStats stats,
		List<Listing> latestListings,
		List<ListingPriceDrop> priceDrops,
		List<ScraperTask>? tasks = null)
	{
		listingRepositoryMock
			.Setup(x => x.GetDashboardStatsAsync(It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(stats);

		listingRepositoryMock
			.Setup(x => x.GetPagedAsync(It.IsAny<bool?>(), It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync((latestListings, latestListings.Count));

		listingRepositoryMock
			.Setup(x => x.GetRecentPriceDropsAsync(It.IsAny<DateTimeOffset>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(priceDrops);

		scraperTaskRepositoryMock
			.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(tasks ?? []);
	}

	private static Listing CreateListing(Guid? scraperTaskId = null, decimal? price = 5_000_000)
	{
		return new Listing
		{
			Id = Guid.NewGuid(),
			ExternalId = "ext-1",
			Title = "Prodej domu",
			Location = "Brno",
			Url = "https://example.com/1",
			ImageUrl = string.Empty,
			Price = price,
			CreatedAt = Now,
			LastSeenAt = Now,
			PriceFrom = Now,
			ScraperTaskId = scraperTaskId
		};
	}

	[Fact]
	public async Task Handle_UsesSevenDayWindowFromCurrentTime()
	{
		// Arrange
		var sut = CreateSut();

		// Act
		var result = await sut.Handle(new GetDashboardSummaryQuery(), CancellationToken.None);

		// Assert
		Assert.True(result.IsSuccess);
		Assert.Equal(7, result.Value.WindowDays);
		listingRepositoryMock.Verify(x => x.GetDashboardStatsAsync(Now.AddDays(-7), It.IsAny<CancellationToken>()), Times.Once);
		listingRepositoryMock.Verify(x => x.GetRecentPriceDropsAsync(Now.AddDays(-7), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task Handle_AsksOnlyForActiveListingsOnFirstPage()
	{
		// Arrange
		var sut = CreateSut();

		// Act
		await sut.Handle(new GetDashboardSummaryQuery(), CancellationToken.None);

		// Assert
		listingRepositoryMock.Verify(x => x.GetPagedAsync(true, null, null, 0, 6, It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task Handle_ReturnsCountsFromRepository()
	{
		// Arrange
		SetupRepositories(new ListingDashboardStats(1248, 37, 22, 14), [], []);
		var sut = CreateSut();

		// Act
		var result = await sut.Handle(new GetDashboardSummaryQuery(), CancellationToken.None);

		// Assert
		Assert.True(result.IsSuccess);
		Assert.Equal(1248, result.Value.ActiveCount);
		Assert.Equal(37, result.Value.NewCount);
		Assert.Equal(22, result.Value.RemovedCount);
		Assert.Equal(14, result.Value.PriceDropCount);
	}

	[Fact]
	public async Task Handle_MapsScraperTaskNameOnLatestListingsAndPriceDrops()
	{
		// Arrange
		var task = new ScraperTask("Můj task", "0 * * * *", true, Now, null)
		{
			Id = Guid.NewGuid()
		};
		var listingWithTask = CreateListing(task.Id);
		var listingWithoutTask = CreateListing();
		var drop = new ListingPriceDrop(CreateListing(task.Id, 2_950_000), 3_200_000);
		SetupRepositories(new ListingDashboardStats(0, 0, 0, 1), [listingWithTask, listingWithoutTask], [drop], [task]);
		var sut = CreateSut();

		// Act
		var result = await sut.Handle(new GetDashboardSummaryQuery(), CancellationToken.None);

		// Assert
		Assert.True(result.IsSuccess);
		Assert.Equal("Můj task", result.Value.LatestListings[0].ScraperTaskName);
		Assert.Null(result.Value.LatestListings[1].ScraperTaskName);
		Assert.Equal("Můj task", result.Value.RecentPriceDrops[0].Listing.ScraperTaskName);
	}

	[Fact]
	public async Task Handle_MapsPreviousPriceOnPriceDrops()
	{
		// Arrange
		var drop = new ListingPriceDrop(CreateListing(price: 2_950_000), 3_200_000);
		SetupRepositories(new ListingDashboardStats(0, 0, 0, 1), [], [drop]);
		var sut = CreateSut();

		// Act
		var result = await sut.Handle(new GetDashboardSummaryQuery(), CancellationToken.None);

		// Assert
		Assert.True(result.IsSuccess);
		var mapped = Assert.Single(result.Value.RecentPriceDrops);
		Assert.Equal(3_200_000, mapped.PreviousPrice);
		Assert.Equal(2_950_000, mapped.Listing.Price);
	}

	[Fact]
	public async Task Handle_ReturnsEmptyListsWhenThereIsNoData()
	{
		// Arrange
		var sut = CreateSut();

		// Act
		var result = await sut.Handle(new GetDashboardSummaryQuery(), CancellationToken.None);

		// Assert
		Assert.True(result.IsSuccess);
		Assert.Empty(result.Value.LatestListings);
		Assert.Empty(result.Value.RecentPriceDrops);
	}
}