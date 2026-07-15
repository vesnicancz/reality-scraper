using Microsoft.Extensions.Logging;
using Moq;
using RealityScraper.Application.Features.Scraping;
using RealityScraper.Application.Features.Scraping.Model.Report;
using RealityScraper.Application.Interfaces.Repositories.Realty;
using RealityScraper.Domain.Entities.Realty;
using RealityScraper.SharedKernel;

namespace RealityScraper.Application.Tests.Features.Scraping;

public class RemovedListingDetectorTests
{
	private static readonly DateTimeOffset Now = new(2026, 7, 15, 12, 0, 0, TimeSpan.Zero);
	private static readonly DateTimeOffset Earlier = Now.AddDays(-3);

	private readonly Mock<IListingRepository> listingRepositoryMock = new();
	private readonly Mock<IDateTimeProvider> dateTimeProviderMock = new();

	private RemovedListingDetector CreateSut()
	{
		dateTimeProviderMock.Setup(x => x.UtcNow).Returns(Now);
		return new RemovedListingDetector(
			listingRepositoryMock.Object,
			dateTimeProviderMock.Object,
			Mock.Of<ILogger<RemovedListingDetector>>());
	}

	private static Listing CreateListing(string externalId, DateTimeOffset? removedAt = null)
	{
		return new Listing
		{
			Id = Guid.NewGuid(),
			ExternalId = externalId,
			Title = "Title",
			Location = "Location",
			Url = "Url",
			ImageUrl = string.Empty,
			CreatedAt = Earlier,
			LastSeenAt = Earlier,
			RemovedAt = removedAt
		};
	}

	private static ScrapingReport CreateReport(Guid taskId, bool succeeded, params string[] seenExternalIds)
	{
		return CreateReport(taskId, succeeded, new List<PortalReport>(), seenExternalIds);
	}

	private static ScrapingReport CreateReport(Guid taskId, bool succeeded, List<PortalReport> results, params string[] seenExternalIds)
	{
		return new ScrapingReport
		{
			ScraperTaskId = taskId,
			TaskName = "task",
			ScrapingSucceeded = succeeded,
			Results = results,
			SeenExternalIds = new HashSet<string>(seenExternalIds)
		};
	}

	[Fact]
	public async Task DetectAsync_UnseenActiveListing_IsMarkedAsRemoved()
	{
		// arrange
		var taskId = Guid.NewGuid();
		var unseen = CreateListing("Unseen");
		var seen = CreateListing("Seen");
		listingRepositoryMock
			.Setup(x => x.GetByScraperTaskIdAsync(taskId, It.IsAny<CancellationToken>()))
			.ReturnsAsync([unseen, seen]);

		var sut = CreateSut();

		// act
		await sut.DetectAsync(CreateReport(taskId, succeeded: true, "Seen"), CancellationToken.None);

		// assert
		Assert.Equal(Now, unseen.RemovedAt);
		Assert.Null(seen.RemovedAt);
	}

	[Fact]
	public async Task DetectAsync_SeenListing_LastSeenAtIsUpdated()
	{
		// arrange
		var taskId = Guid.NewGuid();
		var seen = CreateListing("Seen");
		listingRepositoryMock
			.Setup(x => x.GetByScraperTaskIdAsync(taskId, It.IsAny<CancellationToken>()))
			.ReturnsAsync([seen]);

		var sut = CreateSut();

		// act
		await sut.DetectAsync(CreateReport(taskId, succeeded: true, "Seen"), CancellationToken.None);

		// assert
		Assert.Equal(Now, seen.LastSeenAt);
	}

	[Fact]
	public async Task DetectAsync_RemovedListingReappears_RemovedAtIsReset()
	{
		// arrange
		var taskId = Guid.NewGuid();
		var reappeared = CreateListing("Reappeared", removedAt: Earlier);
		listingRepositoryMock
			.Setup(x => x.GetByScraperTaskIdAsync(taskId, It.IsAny<CancellationToken>()))
			.ReturnsAsync([reappeared]);

		var sut = CreateSut();

		// act
		await sut.DetectAsync(CreateReport(taskId, succeeded: true, "Reappeared"), CancellationToken.None);

		// assert
		Assert.Null(reappeared.RemovedAt);
		Assert.Equal(Now, reappeared.LastSeenAt);
	}

	[Fact]
	public async Task DetectAsync_ScrapingFailed_SeenListingIsUpdatedButNothingIsRemoved()
	{
		// arrange
		var taskId = Guid.NewGuid();
		var unseen = CreateListing("Unseen");
		var seen = CreateListing("Seen");
		listingRepositoryMock
			.Setup(x => x.GetByScraperTaskIdAsync(taskId, It.IsAny<CancellationToken>()))
			.ReturnsAsync([unseen, seen]);

		var sut = CreateSut();

		// act
		await sut.DetectAsync(CreateReport(taskId, succeeded: false, "Seen"), CancellationToken.None);

		// assert
		Assert.Equal(Now, seen.LastSeenAt);
		Assert.Null(unseen.RemovedAt);
		Assert.Equal(Earlier, unseen.LastSeenAt);
	}

	[Fact]
	public async Task DetectAsync_PortalReturnedZeroListings_SeenListingIsUpdatedButNothingIsRemoved()
	{
		// arrange
		var taskId = Guid.NewGuid();
		var unseen = CreateListing("Unseen");
		var seen = CreateListing("Seen");
		listingRepositoryMock
			.Setup(x => x.GetByScraperTaskIdAsync(taskId, It.IsAny<CancellationToken>()))
			.ReturnsAsync([unseen, seen]);

		var results = new List<PortalReport>
		{
			new PortalReport { SiteName = "PortalA", TotalListingsCount = 1 },
			new PortalReport { SiteName = "PortalB", TotalListingsCount = 0 }
		};

		var sut = CreateSut();

		// act
		await sut.DetectAsync(CreateReport(taskId, succeeded: true, results, "Seen"), CancellationToken.None);

		// assert
		Assert.Equal(Now, seen.LastSeenAt);
		Assert.Null(unseen.RemovedAt);
	}

	[Fact]
	public async Task DetectAsync_AllPortalsReturnedListings_UnseenListingIsRemoved()
	{
		// arrange
		var taskId = Guid.NewGuid();
		var unseen = CreateListing("Unseen");
		var seen = CreateListing("Seen");
		listingRepositoryMock
			.Setup(x => x.GetByScraperTaskIdAsync(taskId, It.IsAny<CancellationToken>()))
			.ReturnsAsync([unseen, seen]);

		var results = new List<PortalReport>
		{
			new PortalReport { SiteName = "PortalA", TotalListingsCount = 1 },
			new PortalReport { SiteName = "PortalB", TotalListingsCount = 3 }
		};

		var sut = CreateSut();

		// act
		await sut.DetectAsync(CreateReport(taskId, succeeded: true, results, "Seen"), CancellationToken.None);

		// assert
		Assert.Equal(Now, unseen.RemovedAt);
	}

	[Fact]
	public async Task DetectAsync_NoSeenListingsButActiveInDatabase_NothingIsMarked()
	{
		// arrange
		var taskId = Guid.NewGuid();
		var active = CreateListing("Active");
		listingRepositoryMock
			.Setup(x => x.GetByScraperTaskIdAsync(taskId, It.IsAny<CancellationToken>()))
			.ReturnsAsync([active]);

		var sut = CreateSut();

		// act
		await sut.DetectAsync(CreateReport(taskId, succeeded: true), CancellationToken.None);

		// assert
		Assert.Null(active.RemovedAt);
	}

	[Fact]
	public async Task DetectAsync_AlreadyRemovedListingStillUnseen_RemovedAtIsNotOverwritten()
	{
		// arrange
		var taskId = Guid.NewGuid();
		var removed = CreateListing("Removed", removedAt: Earlier);
		var seen = CreateListing("Seen");
		listingRepositoryMock
			.Setup(x => x.GetByScraperTaskIdAsync(taskId, It.IsAny<CancellationToken>()))
			.ReturnsAsync([removed, seen]);

		var sut = CreateSut();

		// act
		await sut.DetectAsync(CreateReport(taskId, succeeded: true, "Seen"), CancellationToken.None);

		// assert
		Assert.Equal(Earlier, removed.RemovedAt);
	}
}
