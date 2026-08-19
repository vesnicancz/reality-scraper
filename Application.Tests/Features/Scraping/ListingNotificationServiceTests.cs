using Microsoft.Extensions.Logging;
using Moq;
using RealityScraper.Application.Features.Scraping;
using RealityScraper.Application.Features.Scraping.Model.Report;
using RealityScraper.Application.Interfaces.Mailing;

namespace RealityScraper.Application.Tests.Features.Scraping;

public class ListingNotificationServiceTests
{
	private static readonly List<string> Recipients = ["user@example.com"];

	private readonly Mock<IMailerService> mailerServiceMock = new();

	private ListingNotificationService CreateSut()
	{
		return new ListingNotificationService(
			mailerServiceMock.Object,
			Mock.Of<ILogger<ListingNotificationService>>());
	}

	private static ListingItem NewItem(string externalId = "ext-1")
	{
		return new ListingItem
		{
			Title = "Prodej domu",
			Price = 5_000_000,
			Location = "Brno",
			Url = "https://example.com/1",
			ImageUrl = "https://example.com/1.jpg",
			ExternalId = externalId
		};
	}

	private static ListingItemWithNewPrice PriceChangedItem(string externalId = "ext-2")
	{
		return new ListingItemWithNewPrice
		{
			Title = "Prodej bytu",
			Price = 4_500_000,
			OldPrice = 5_000_000,
			Location = "Brno",
			Url = "https://example.com/2",
			ImageUrl = "https://example.com/2.jpg",
			ExternalId = externalId
		};
	}

	private static ScrapingReport CreateReport(
		List<ListingItem>? newListings = null,
		List<ListingItemWithNewPrice>? priceChanged = null)
	{
		return new ScrapingReport
		{
			ScraperTaskId = Guid.NewGuid(),
			TaskName = "Byty Brno",
			ScrapingSucceeded = true,
			Results =
			[
				new PortalReport
				{
					SiteName = "sreality",
					NewListings = newListings ?? [],
					PriceChangedListings = priceChanged ?? []
				}
			]
		};
	}

	[Fact]
	public async Task SendNotificationsAsync_DoesNotSendMail_WhenThereAreNoChanges()
	{
		// Arrange
		var sut = CreateSut();

		// Act
		await sut.SendNotificationsAsync(CreateReport(), Recipients, CancellationToken.None);

		// Assert
		mailerServiceMock.Verify(
			x => x.SendListingReportAsync(It.IsAny<ScrapingReport>(), It.IsAny<List<string>>(), It.IsAny<CancellationToken>()),
			Times.Never);
	}

	[Fact]
	public async Task SendNotificationsAsync_SendsMail_WhenThereAreNewListings()
	{
		// Arrange
		var report = CreateReport(newListings: [NewItem()]);
		mailerServiceMock
			.Setup(x => x.SendListingReportAsync(report, Recipients, It.IsAny<CancellationToken>()))
			.ReturnsAsync(true);
		var sut = CreateSut();

		// Act
		await sut.SendNotificationsAsync(report, Recipients, CancellationToken.None);

		// Assert
		mailerServiceMock.Verify(
			x => x.SendListingReportAsync(report, Recipients, It.IsAny<CancellationToken>()),
			Times.Once);
	}

	[Fact]
	public async Task SendNotificationsAsync_SendsMail_WhenOnlyPricesChanged()
	{
		// Arrange
		var report = CreateReport(priceChanged: [PriceChangedItem()]);
		mailerServiceMock
			.Setup(x => x.SendListingReportAsync(report, Recipients, It.IsAny<CancellationToken>()))
			.ReturnsAsync(true);
		var sut = CreateSut();

		// Act
		await sut.SendNotificationsAsync(report, Recipients, CancellationToken.None);

		// Assert
		mailerServiceMock.Verify(
			x => x.SendListingReportAsync(report, Recipients, It.IsAny<CancellationToken>()),
			Times.Once);
	}

	[Fact]
	public async Task SendNotificationsAsync_Throws_WhenMailWasNotSent()
	{
		// Arrange
		// Výjimka je záměr - zabrání uložení inzerátů jako viděných, takže další běh notifikaci zopakuje.
		var report = CreateReport(newListings: [NewItem()]);
		mailerServiceMock
			.Setup(x => x.SendListingReportAsync(It.IsAny<ScrapingReport>(), It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(false);
		var sut = CreateSut();

		// Act & Assert
		var exception = await Assert.ThrowsAsync<InvalidOperationException>(
			() => sut.SendNotificationsAsync(report, Recipients, CancellationToken.None));
		Assert.Contains("Byty Brno", exception.Message);
	}
}
