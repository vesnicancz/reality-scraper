using Microsoft.Extensions.Logging;
using Moq;
using RealityScraper.Application.Features.Scraping.Builders;
using RealityScraper.Application.Features.Scraping.Model;
using RealityScraper.Application.Interfaces.Repositories.Realty;

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
				ExternalId = "ExternalId1"
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
				ExternalId = "ExternalId1"
			}
		};

		var listingRepositoryMock = new Mock<IListingRepository>();
		var loggerMock = new Mock<ILogger<ScrapingReportBuilder>>();

		var sut = new ScrapingReportBuilder(listingRepositoryMock.Object, loggerMock.Object);

		// act
		await sut.ProcessScraperResultsAsync("siteName", listings1, CancellationToken.None);
		await sut.ProcessScraperResultsAsync("siteName", listings2, CancellationToken.None);
		var result = sut.Build();

		// assert

		Assert.Equal(1, result.NewListingsCount);
		Assert.Equal(1, result.TotalListingsCount);
	}
}