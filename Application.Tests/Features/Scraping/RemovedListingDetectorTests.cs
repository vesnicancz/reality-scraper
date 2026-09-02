using Microsoft.Extensions.Logging;
using Moq;
using RealityScraper.Application.Features.Scraping;
using RealityScraper.Application.Features.Scraping.Model;
using RealityScraper.Application.Features.Scraping.Model.Report;
using RealityScraper.Application.Interfaces.Repositories.Realty;
using RealityScraper.Domain.Entities.Realty;
using RealityScraper.SharedKernel;

namespace RealityScraper.Application.Tests.Features.Scraping;

public class RemovedListingDetectorTests
{
	private static readonly DateTimeOffset Now = new(2026, 7, 15, 12, 0, 0, TimeSpan.Zero);

	/// <summary>Okamžik minulého běhu - inzerát s touto hodnotou LastSeenAt tam ještě byl.</summary>
	private static readonly DateTimeOffset Earlier = Now.AddDays(-3);

	/// <summary>Starší než minulý běh - inzerát chybí už podruhé v řadě.</summary>
	private static readonly DateTimeOffset LongGone = Now.AddDays(-5);

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

	private static Listing CreateListing(string externalId, DateTimeOffset? removedAt = null, DateTimeOffset? lastSeenAt = null)
	{
		return new Listing
		{
			Id = Guid.NewGuid(),
			ExternalId = externalId,
			Title = "Title",
			Location = "Location",
			Url = "Url",
			ImageUrl = string.Empty,
			CreatedAt = LongGone,
			LastSeenAt = lastSeenAt ?? Earlier,
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
			SeenListings = seenExternalIds.ToDictionary(
				externalId => externalId,
				externalId => new ScraperListingItem
				{
					Title = "Title",
					Location = "Location",
					Url = "Url",
					ImageUrl = string.Empty,
					ExternalId = externalId
				})
		};
	}

	private void SetupListings(Guid taskId, params Listing[] listings)
	{
		listingRepositoryMock
			.Setup(x => x.GetByScraperTaskIdAsync(taskId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(listings.ToList());
	}

	[Fact]
	public async Task DetectAsync_UnseenActiveListing_IsMarkedAsRemoved()
	{
		// arrange
		var taskId = Guid.NewGuid();
		var unseen = CreateListing("Unseen");
		var seen = CreateListing("Seen");
		SetupListings(taskId, unseen, seen);

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
		SetupListings(taskId, seen);

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
		SetupListings(taskId, reappeared);

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
		var unseen = CreateListing("Unseen", lastSeenAt: LongGone);
		var seen = CreateListing("Seen");
		SetupListings(taskId, unseen, seen);

		var sut = CreateSut();

		// act
		await sut.DetectAsync(CreateReport(taskId, succeeded: false, "Seen"), CancellationToken.None);

		// assert
		Assert.Equal(Now, seen.LastSeenAt);
		Assert.Null(unseen.RemovedAt);
		Assert.Equal(LongGone, unseen.LastSeenAt);
	}

	[Fact]
	public async Task DetectAsync_NoSeenListingsButActiveInDatabase_NothingIsMarked()
	{
		// arrange
		var taskId = Guid.NewGuid();
		var active = CreateListing("Active", lastSeenAt: LongGone);
		SetupListings(taskId, active);

		var sut = CreateSut();

		// act
		await sut.DetectAsync(CreateReport(taskId, succeeded: true), CancellationToken.None);

		// assert
		Assert.Null(active.RemovedAt);
	}

	[Fact]
	public async Task DetectAsync_AllPortalsReturnedListings_UnseenListingIsRemoved()
	{
		// arrange
		var taskId = Guid.NewGuid();
		var unseen = CreateListing("Unseen");
		var seen = CreateListing("Seen");
		SetupListings(taskId, unseen, seen);

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
	public async Task DetectAsync_AlreadyRemovedListingStillUnseen_RemovedAtIsNotOverwritten()
	{
		// arrange
		var taskId = Guid.NewGuid();
		var removed = CreateListing("Removed", removedAt: Earlier, lastSeenAt: LongGone);
		var seen = CreateListing("Seen");
		SetupListings(taskId, removed, seen);

		var sut = CreateSut();

		// act
		await sut.DetectAsync(CreateReport(taskId, succeeded: true, "Seen"), CancellationToken.None);

		// assert
		Assert.Equal(Earlier, removed.RemovedAt);
	}

	// --- Opatrný režim: dílčí anomálie odloží vyřazení o jeden běh, ale nevypnou detekci ---

	[Fact]
	public async Task DetectAsync_PortalReturnedZeroListings_FreshlyMissingListingIsPostponed()
	{
		// arrange
		var taskId = Guid.NewGuid();
		var unseen = CreateListing("Unseen");
		var seen = CreateListing("Seen");
		SetupListings(taskId, unseen, seen);

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
	public async Task DetectAsync_SomeListingsFailedToParse_FreshlyMissingListingIsPostponed()
	{
		// arrange
		var taskId = Guid.NewGuid();
		var unseen = CreateListing("Unseen");
		var seen = CreateListing("Seen");
		SetupListings(taskId, unseen, seen);

		var report = CreateReport(taskId, succeeded: true, "Seen") with { FailedListingsCount = 1 };

		var sut = CreateSut();

		// act
		await sut.DetectAsync(report, CancellationToken.None);

		// assert
		Assert.Equal(Now, seen.LastSeenAt);
		Assert.Null(unseen.RemovedAt);
	}

	[Fact]
	public async Task DetectAsync_SomeTargetReturnedZeroListings_FreshlyMissingListingIsPostponed()
	{
		// arrange
		var taskId = Guid.NewGuid();
		var unseen = CreateListing("Unseen");
		var seen = CreateListing("Seen");
		SetupListings(taskId, unseen, seen);

		var results = new List<PortalReport>
		{
			new PortalReport { SiteName = "PortalA", TotalListingsCount = 2 }
		};

		var report = CreateReport(taskId, succeeded: true, results, "Seen") with { AnyTargetEmpty = true };

		var sut = CreateSut();

		// act
		await sut.DetectAsync(report, CancellationToken.None);

		// assert
		Assert.Equal(Now, seen.LastSeenAt);
		Assert.Null(unseen.RemovedAt);
	}

	/// <summary>
	/// Reklamní bloky mezi inzeráty se přeskakují při každém běhu a do databáze nevstoupí, takže
	/// jejich obvyklý podíl nesmí zdržovat vyřazování. Na SReality jde o jednotky procent karet.
	/// </summary>
	[Fact]
	public async Task DetectAsync_FewSkippedCards_FreshlyMissingListingIsRemovedImmediately()
	{
		// arrange
		var taskId = Guid.NewGuid();
		var unseen = CreateListing("Unseen");
		var seen = CreateListing("Seen");
		SetupListings(taskId, unseen, seen);

		var results = new List<PortalReport>
		{
			new PortalReport { SiteName = "PortalA", TotalListingsCount = 344 }
		};

		var report = CreateReport(taskId, succeeded: true, results, "Seen") with { SkippedListingsCount = 17 };

		var sut = CreateSut();

		// act
		await sut.DetectAsync(report, CancellationToken.None);

		// assert
		Assert.Equal(Now, unseen.RemovedAt);
	}

	/// <summary>
	/// Nepoměr znamená, že portál nejspíš změnil tvar URL detailu - pak neprocházejí ani pravé
	/// inzeráty a chybějící karta nesmí hned znamenat vyřazení.
	/// </summary>
	[Fact]
	public async Task DetectAsync_DisproportionateSkippedCards_FreshlyMissingListingIsPostponed()
	{
		// arrange
		var taskId = Guid.NewGuid();
		var unseen = CreateListing("Unseen");
		var seen = CreateListing("Seen");
		SetupListings(taskId, unseen, seen);

		var results = new List<PortalReport>
		{
			new PortalReport { SiteName = "PortalA", TotalListingsCount = 2 }
		};

		var report = CreateReport(taskId, succeeded: true, results, "Seen") with { SkippedListingsCount = 10 };

		var sut = CreateSut();

		// act
		await sut.DetectAsync(report, CancellationToken.None);

		// assert
		Assert.Null(unseen.RemovedAt);
	}

	/// <summary>
	/// Regrese na tichý výpadek reportu vyřazených: prázdný cíl (úzký filtr, malá obec) dřív vypnul
	/// detekci pro celou úlohu, takže se vyřazení nezaznamenalo ani po týdnu. Nově se odloží jen
	/// o jeden běh - inzerát chybějící i v minulém běhu se vyřadí bez ohledu na prázdný cíl.
	/// </summary>
	[Fact]
	public async Task DetectAsync_EmptyTargetButListingMissingSinceBeforePreviousRun_IsRemoved()
	{
		// arrange
		var taskId = Guid.NewGuid();
		var longMissing = CreateListing("LongMissing", lastSeenAt: LongGone);
		var freshlyMissing = CreateListing("FreshlyMissing");
		var seen = CreateListing("Seen");
		SetupListings(taskId, longMissing, freshlyMissing, seen);

		var results = new List<PortalReport>
		{
			new PortalReport { SiteName = "PortalA", TotalListingsCount = 2 }
		};

		var report = CreateReport(taskId, succeeded: true, results, "Seen") with { AnyTargetEmpty = true };

		var sut = CreateSut();

		// act
		await sut.DetectAsync(report, CancellationToken.None);

		// assert
		Assert.Equal(Now, longMissing.RemovedAt);
		Assert.Null(freshlyMissing.RemovedAt);
	}

	[Fact]
	public async Task DetectAsync_FailedListingsButListingMissingSinceBeforePreviousRun_IsRemoved()
	{
		// arrange
		var taskId = Guid.NewGuid();
		var longMissing = CreateListing("LongMissing", lastSeenAt: LongGone);
		var seen = CreateListing("Seen");
		SetupListings(taskId, longMissing, seen);

		var report = CreateReport(taskId, succeeded: true, "Seen") with { FailedListingsCount = 4 };

		var sut = CreateSut();

		// act
		await sut.DetectAsync(report, CancellationToken.None);

		// assert
		Assert.Equal(Now, longMissing.RemovedAt);
	}

	[Fact]
	public async Task DetectAsync_DisproportionateSkippedCardsButListingMissingSinceBeforePreviousRun_IsRemoved()
	{
		// arrange
		var taskId = Guid.NewGuid();
		var longMissing = CreateListing("LongMissing", lastSeenAt: LongGone);
		var seen = CreateListing("Seen");
		SetupListings(taskId, longMissing, seen);

		var results = new List<PortalReport>
		{
			new PortalReport { SiteName = "PortalA", TotalListingsCount = 2 }
		};

		var report = CreateReport(taskId, succeeded: true, results, "Seen") with { SkippedListingsCount = 10 };

		var sut = CreateSut();

		// act
		await sut.DetectAsync(report, CancellationToken.None);

		// assert
		Assert.Equal(Now, longMissing.RemovedAt);
	}
}
