using Microsoft.Extensions.Logging;
using Moq;
using RealityScraper.Application.Features.Scraping;
using RealityScraper.Application.Features.Scraping.Configuration;
using RealityScraper.Application.Features.Scraping.Model;
using RealityScraper.Application.Interfaces;
using RealityScraper.Application.Interfaces.Mailing;
using RealityScraper.Application.Interfaces.Repositories.Realty;
using RealityScraper.Application.Interfaces.Scraping;
using RealityScraper.Domain.Entities.Realty;
using RealityScraper.Domain.Enums;

namespace RealityScraper.Application.Tests.Features.Scraping;

public class ScraperServiceTaskTests
{
	[Fact]
	public async Task ScraperServiceTask_ExecuteAsync_DuplicitListingBetweenTargets()
	{
		// arrange
		const string url1 = "https://local.address/1";
		const string url2 = "https://local.address/2";

		var configuration = new ScrapingConfiguration
		{
			Scrapers =
			{
				new ScraperConfiguration
				{
					ScraperType = ScrapersEnum.SReality,
					Url = url1
				},
				new ScraperConfiguration
				{
					ScraperType = ScrapersEnum.SReality,
					Url = url2
				}
			}
		};

		var scraperServiceMock = new Mock<IRealityScraperService>();
		var scraperServices = new List<IRealityScraperService> { scraperServiceMock.Object };
		var mailerServiceMock = new Mock<IMailerService>();
		var imageDownloadServiceMock = new Mock<IImageDownloadService>();
		var listingRepositoryMock = new Mock<IListingRepository>();
		var unitOfWorkMock = new Mock<IUnitOfWork>();
		var loggerMock = new Mock<ILogger<ScraperServiceTask>>();

		scraperServiceMock.Setup(i => i.ScrapersEnum).Returns(ScrapersEnum.SReality);
		var result1 = new List<ListingItem>
		{
			new ListingItem
			{
				Title = "Title1",
				Price = 1000,
				Location = "Location1",
				Url = "Url1",
				ExternalId = "ExternalId1"
			}
		};
		scraperServiceMock
			.Setup(i => i.ScrapeListingsAsync(It.Is<ScraperConfiguration>(j => j.Url == url1), It.IsAny<CancellationToken>()))
			.ReturnsAsync(result1);

		var result2 = new List<ListingItem>
		{
			new ListingItem
			{
				Title = "Title1",
				Price = 1000,
				Location = "Location1",
				Url = "Url1",
				ExternalId = "ExternalId1"
			}
		};
		scraperServiceMock
			.Setup(i => i.ScrapeListingsAsync(It.Is<ScraperConfiguration>(j => j.Url == url2), It.IsAny<CancellationToken>()))
			.ReturnsAsync(result2);

		var scraperServiceTask = new ScraperServiceTask(scraperServices, mailerServiceMock.Object, imageDownloadServiceMock.Object, listingRepositoryMock.Object, unitOfWorkMock.Object, loggerMock.Object);

		// act
		await scraperServiceTask.ExecuteAsync(configuration, CancellationToken.None);

		// assert
		listingRepositoryMock.Verify(i => i.AddAsync(It.IsAny<Listing>(), It.IsAny<CancellationToken>()), Times.Once);
	}
}