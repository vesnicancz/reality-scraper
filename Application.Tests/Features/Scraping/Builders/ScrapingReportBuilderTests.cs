using Microsoft.Extensions.Logging;
using Moq;
using RealityScraper.Application.Features.Scraping.Builders;
using RealityScraper.Application.Features.Scraping.Model;
using RealityScraper.Application.Interfaces.Repositories.Realty;
using RealityScraper.Domain.Entities.Realty;
using RealityScraper.SharedKernel;

namespace RealityScraper.Application.Tests.Features.Scraping.Builders;

public class ScrapingReportBuilderTests
{
	[Fact]
	public async Task ScrapingReportBuilder_ProcessScraperResults_DuplicitListingBetweenTargets()
	{
		// arrange
		var listings1 = new List<ScraperListingItem>
		{
			new ScraperListingItem
			{
				Title = "Title1",
				Price = 1000,
				Location = "Location1",
				Url = "Url1",
				ExternalId = "ExternalId1",
				ImageUrl = string.Empty
			}
		};

		var listings2 = new List<ScraperListingItem>
		{
			new ScraperListingItem
			{
				Title = "Title1",
				Price = 1000,
				Location = "Location1",
				Url = "Url1",
				ExternalId = "ExternalId1",
				ImageUrl = string.Empty
			}
		};

		var sut = CreateBuilder();

		// act
		await sut.ProcessScraperResultsAsync("siteName", new ScraperRunResult(true, listings1), CancellationToken.None);
		await sut.ProcessScraperResultsAsync("siteName", new ScraperRunResult(true, listings2), CancellationToken.None);
		var result = sut.Build();

		// assert

		Assert.Equal(1, result.NewListingsCount);
		Assert.Equal(1, result.TotalListingsCount);
	}

	[Fact]
	public async Task ScrapingReportBuilder_ProcessScraperResults_SameExternalIdOnDifferentSitesIsNotDeduplicated()
	{
		// arrange
		var listings1 = new List<ScraperListingItem>
		{
			new ScraperListingItem { Title = "T1", Price = 1000, Location = "L1", Url = "U1", ExternalId = "Ext1", ImageUrl = string.Empty }
		};

		var listings2 = new List<ScraperListingItem>
		{
			new ScraperListingItem { Title = "T1", Price = 1000, Location = "L1", Url = "U2", ExternalId = "Ext1", ImageUrl = string.Empty }
		};

		var sut = CreateBuilder();
		sut.ForScrapingReport(Guid.NewGuid(), "task");

		// act
		await sut.ProcessScraperResultsAsync("siteA", new ScraperRunResult(true, listings1), CancellationToken.None);
		await sut.ProcessScraperResultsAsync("siteB", new ScraperRunResult(true, listings2), CancellationToken.None);
		var result = sut.Build();

		// assert
		Assert.Equal(2, result.NewListingsCount);
		Assert.Equal(2, result.TotalListingsCount);
		Assert.All(result.Results, r => Assert.Equal(1, r.TotalListingsCount));
		Assert.Equal(new HashSet<string> { "Ext1" }, result.SeenExternalIds);
	}

	[Fact]
	public async Task ScrapingReportBuilder_ProcessScraperResults_TotalCountSumsAcrossTargetsOfSameSite()
	{
		// arrange
		var listings1 = new List<ScraperListingItem>
		{
			new ScraperListingItem { Title = "T1", Price = 1000, Location = "L1", Url = "U1", ExternalId = "Ext1", ImageUrl = string.Empty },
			new ScraperListingItem { Title = "T2", Price = 2000, Location = "L2", Url = "U2", ExternalId = "Ext2", ImageUrl = string.Empty }
		};

		var listings2 = new List<ScraperListingItem>
		{
			new ScraperListingItem { Title = "T3", Price = 3000, Location = "L3", Url = "U3", ExternalId = "Ext3", ImageUrl = string.Empty }
		};

		var sut = CreateBuilder();
		sut.ForScrapingReport(Guid.NewGuid(), "task");

		// act
		await sut.ProcessScraperResultsAsync("siteName", new ScraperRunResult(true, listings1), CancellationToken.None);
		await sut.ProcessScraperResultsAsync("siteName", new ScraperRunResult(true, listings2), CancellationToken.None);
		var result = sut.Build();

		// assert
		Assert.Equal(3, result.TotalListingsCount);
		Assert.Equal(3, result.NewListingsCount);
	}

	[Fact]
	public async Task ScrapingReportBuilder_ProcessScraperResults_UnchangedExistingListingCountsIntoTotal()
	{
		// arrange
		var listings = new List<ScraperListingItem>
		{
			new ScraperListingItem { Title = "T1", Price = 1000, Location = "L1", Url = "U1", ExternalId = "Ext1", ImageUrl = string.Empty }
		};

		var sut = CreateBuilder(new List<Listing> { new Listing { ExternalId = "Ext1", Price = 1000 } });
		sut.ForScrapingReport(Guid.NewGuid(), "task");

		// act
		await sut.ProcessScraperResultsAsync("siteName", new ScraperRunResult(true, listings), CancellationToken.None);
		var result = sut.Build();

		// assert
		Assert.Equal(1, result.TotalListingsCount);
		Assert.Equal(0, result.NewListingsCount);
		Assert.Equal(0, result.PriceChangedListingsCount);
	}

	[Fact]
	public async Task ScrapingReportBuilder_ProcessScraperResults_NullPriceOnExistingListingIsNotPriceChange()
	{
		// arrange - portál zobrazí "Cena na dotaz" u inzerátu s uloženou cenou
		var listings = new List<ScraperListingItem>
		{
			new ScraperListingItem { Title = "T1", Price = null, Location = "L1", Url = "U1", ExternalId = "Ext1", ImageUrl = string.Empty }
		};

		var sut = CreateBuilder(new List<Listing> { new Listing { ExternalId = "Ext1", Price = 1000 } });
		sut.ForScrapingReport(Guid.NewGuid(), "task");

		// act
		await sut.ProcessScraperResultsAsync("siteName", new ScraperRunResult(true, listings), CancellationToken.None);
		var result = sut.Build();

		// assert
		Assert.Equal(0, result.PriceChangedListingsCount);
		Assert.Equal(0, result.NewListingsCount);
		Assert.Equal(1, result.TotalListingsCount);
	}

	[Fact]
	public async Task ScrapingReportBuilder_Build_PropagatesFailedListingsCountAndEmptyTarget()
	{
		// arrange
		var sut = CreateBuilder();
		sut.ForScrapingReport(Guid.NewGuid(), "task");

		// act
		await sut.ProcessScraperResultsAsync("siteA", new ScraperRunResult(true, new List<ScraperListingItem>(), FailedListingsCount: 2), CancellationToken.None);
		var result = sut.Build();

		// assert
		Assert.Equal(2, result.FailedListingsCount);
		Assert.True(result.AnyTargetEmpty);
	}

	[Fact]
	public async Task ScrapingReportBuilder_Build_PropagatesSuccessAndSeenExternalIds()
	{
		// arrange
		var listings = new List<ScraperListingItem>
		{
			new ScraperListingItem { Title = "T1", Price = 1000, Location = "L1", Url = "U1", ExternalId = "Ext1", ImageUrl = string.Empty },
			new ScraperListingItem { Title = "T2", Price = 2000, Location = "L2", Url = "U2", ExternalId = "Ext2", ImageUrl = string.Empty }
		};

		var sut = CreateBuilder();
		sut.ForScrapingReport(Guid.NewGuid(), "task");

		// act
		await sut.ProcessScraperResultsAsync("siteName", new ScraperRunResult(true, listings), CancellationToken.None);
		var result = sut.Build();

		// assert
		Assert.True(result.ScrapingSucceeded);
		Assert.Equal(new HashSet<string> { "Ext1", "Ext2" }, result.SeenExternalIds);
	}

	[Fact]
	public async Task ScrapingReportBuilder_Build_FailedScraperMarksReportAsUnsuccessful()
	{
		// arrange
		var sut = CreateBuilder();
		sut.ForScrapingReport(Guid.NewGuid(), "task");

		// act
		await sut.ProcessScraperResultsAsync("siteName", new ScraperRunResult(false, new List<ScraperListingItem>()), CancellationToken.None);
		var result = sut.Build();

		// assert
		Assert.False(result.ScrapingSucceeded);
	}

	[Fact]
	public void ScrapingReportBuilder_Build_MarkScraperFailedMarksReportAsUnsuccessful()
	{
		// arrange
		var sut = CreateBuilder();
		sut.ForScrapingReport(Guid.NewGuid(), "task");

		// act
		sut.MarkScraperFailed();
		var result = sut.Build();

		// assert
		Assert.False(result.ScrapingSucceeded);
	}

	[Fact]
	public void ScrapingReportBuilder_ForScrapingReport_ResetsSuccessFlag()
	{
		// arrange
		var sut = CreateBuilder();
		sut.ForScrapingReport(Guid.NewGuid(), "task");
		sut.MarkScraperFailed();

		// act
		sut.ForScrapingReport(Guid.NewGuid(), "task2");
		var result = sut.Build();

		// assert
		Assert.True(result.ScrapingSucceeded);
	}

	private static ScrapingReportBuilder CreateBuilder(List<Listing>? existingListings = null)
	{
		var listingRepositoryMock = new Mock<IListingRepository>();
		listingRepositoryMock
			.Setup(r => r.GetByScraperTaskIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(existingListings ?? new List<Listing>());
		var dateTimeProviderMock = new Mock<IDateTimeProvider>();
		var loggerMock = new Mock<ILogger<ScrapingReportBuilder>>();

		return new ScrapingReportBuilder(listingRepositoryMock.Object, dateTimeProviderMock.Object, loggerMock.Object);
	}
}