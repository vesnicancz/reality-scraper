using Moq;
using RealityScraper.Application.Features.Listings.GetList;
using RealityScraper.Application.Interfaces.Repositories.Configuration;
using RealityScraper.Application.Interfaces.Repositories.Realty;
using RealityScraper.Domain.Entities.Realty;
using RealityScraper.Domain.Entities.Tasks;

namespace RealityScraper.Application.Tests.Features.Listings;

public class GetListingsQueryHandlerTests
{
	private static readonly DateTimeOffset Now = new(2026, 7, 15, 12, 0, 0, TimeSpan.Zero);

	private readonly Mock<IListingRepository> listingRepositoryMock = new();
	private readonly Mock<IScraperTaskRepository> scraperTaskRepositoryMock = new();

	private GetListingsQueryHandler CreateSut()
	{
		return new GetListingsQueryHandler(listingRepositoryMock.Object, scraperTaskRepositoryMock.Object);
	}

	private static Listing CreateListing(Guid? scraperTaskId = null)
	{
		return new Listing
		{
			Id = Guid.NewGuid(),
			ExternalId = "ext-1",
			Title = "Prodej domu",
			Location = "Brno",
			Url = "https://example.com/1",
			ImageUrl = string.Empty,
			CreatedAt = Now,
			LastSeenAt = Now,
			ScraperTaskId = scraperTaskId
		};
	}

	private void SetupRepositories(List<Listing> listings, int totalCount, List<ScraperTask>? tasks = null)
	{
		listingRepositoryMock
			.Setup(x => x.GetPagedAsync(It.IsAny<bool?>(), It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync((listings, totalCount));

		scraperTaskRepositoryMock
			.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(tasks ?? []);
	}

	[Fact]
	public async Task Handle_PassesFiltersAndPagingToRepository()
	{
		// Arrange
		var taskId = Guid.NewGuid();
		SetupRepositories([], 0);
		var sut = CreateSut();

		// Act
		var result = await sut.Handle(new GetListingsQuery(true, taskId, "dům", 2, 20), CancellationToken.None);

		// Assert
		Assert.True(result.IsSuccess);
		listingRepositoryMock.Verify(x => x.GetPagedAsync(true, taskId, "dům", 40, 20, It.IsAny<CancellationToken>()), Times.Once);
	}

	[Theory]
	[InlineData(-5, 0, 0, 1)]
	[InlineData(0, 500, 0, 100)]
	[InlineData(3, 1000, 300, 100)]
	public async Task Handle_ClampsPageIndexAndPageSize(int pageIndex, int pageSize, int expectedSkip, int expectedTake)
	{
		// Arrange
		SetupRepositories([], 0);
		var sut = CreateSut();

		// Act
		await sut.Handle(new GetListingsQuery(null, null, null, pageIndex, pageSize), CancellationToken.None);

		// Assert
		listingRepositoryMock.Verify(x => x.GetPagedAsync(null, null, null, expectedSkip, expectedTake, It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task Handle_MapsScraperTaskName()
	{
		// Arrange
		var task = new ScraperTask("Můj task", "0 * * * *", true, Now, null)
		{
			Id = Guid.NewGuid()
		};
		var listingWithTask = CreateListing(task.Id);
		var listingWithoutTask = CreateListing();
		SetupRepositories([listingWithTask, listingWithoutTask], 2, [task]);
		var sut = CreateSut();

		// Act
		var result = await sut.Handle(new GetListingsQuery(null, null, null, 0, 15), CancellationToken.None);

		// Assert
		Assert.True(result.IsSuccess);
		Assert.Equal(2, result.Value.TotalCount);
		Assert.Equal("Můj task", result.Value.Items[0].ScraperTaskName);
		Assert.Null(result.Value.Items[1].ScraperTaskName);
	}

	[Fact]
	public async Task Handle_ReturnsTotalCountFromRepository()
	{
		// Arrange
		SetupRepositories([CreateListing()], 123);
		var sut = CreateSut();

		// Act
		var result = await sut.Handle(new GetListingsQuery(null, null, null, 0, 15), CancellationToken.None);

		// Assert
		Assert.True(result.IsSuccess);
		Assert.Single(result.Value.Items);
		Assert.Equal(123, result.Value.TotalCount);
	}
}
