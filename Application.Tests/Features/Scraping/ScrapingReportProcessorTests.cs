using Microsoft.Extensions.Logging;
using Moq;
using RealityScraper.Application.Abstractions.Database;
using RealityScraper.Application.Features.Scraping;
using RealityScraper.Application.Features.Scraping.Model.Report;
using RealityScraper.Domain.Entities.Realty;

namespace RealityScraper.Application.Tests.Features.Scraping;

public class ScrapingReportProcessorTests
{
	private readonly Mock<IListingChangeProcessor> changeProcessorMock = new();
	private readonly Mock<IRemovedListingDetector> removedDetectorMock = new();
	private readonly Mock<IListingNotificationService> notificationServiceMock = new();
	private readonly Mock<IListingImageDownloader> imageDownloaderMock = new();
	private readonly Mock<IUnitOfWork> unitOfWorkMock = new();

	private readonly List<string> steps = new();

	private static readonly ScrapingReport Report = new()
	{
		ScraperTaskId = Guid.NewGuid(),
		TaskName = "task",
		ScrapingSucceeded = true
	};

	private static readonly List<string> Recipients = ["user@example.com"];

	private ScrapingReportProcessor CreateSut()
	{
		return new ScrapingReportProcessor(
			changeProcessorMock.Object,
			removedDetectorMock.Object,
			notificationServiceMock.Object,
			imageDownloaderMock.Object,
			unitOfWorkMock.Object,
			Mock.Of<ILogger<ScrapingReportProcessor>>());
	}

	/// <summary>
	/// Zaznamená pořadí, ve kterém procesor volá jednotlivé kroky.
	/// </summary>
	private void RecordStepOrder(List<Listing>? listingsToDownload = null)
	{
		changeProcessorMock
			.Setup(x => x.ProcessChangesAsync(It.IsAny<ScrapingReport>(), It.IsAny<CancellationToken>()))
			.Callback(() => steps.Add("changes"))
			.ReturnsAsync(listingsToDownload ?? []);

		removedDetectorMock
			.Setup(x => x.DetectAsync(It.IsAny<ScrapingReport>(), It.IsAny<CancellationToken>()))
			.Callback(() => steps.Add("removed"))
			.Returns(Task.CompletedTask);

		notificationServiceMock
			.Setup(x => x.SendNotificationsAsync(It.IsAny<ScrapingReport>(), It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
			.Callback(() => steps.Add("notify"))
			.Returns(Task.CompletedTask);

		unitOfWorkMock
			.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
			.Callback(() => steps.Add("save"))
			.Returns(Task.CompletedTask);

		imageDownloaderMock
			.Setup(x => x.DownloadImagesAsync(It.IsAny<List<Listing>>(), It.IsAny<CancellationToken>()))
			.Callback(() => steps.Add("images"))
			.Returns(Task.CompletedTask);
	}

	[Fact]
	public async Task ProcessReportAsync_RunsStepsInOrder_WithImagesDownloadedAfterSave()
	{
		// Arrange
		// Obrázky se smí stahovat až po uložení - do té doby nemají inzeráty přidělené Id.
		RecordStepOrder();
		var sut = CreateSut();

		// Act
		await sut.ProcessReportAsync(Report, Recipients, CancellationToken.None);

		// Assert
		Assert.Equal(["changes", "removed", "notify", "save", "images"], steps);
	}

	[Fact]
	public async Task ProcessReportAsync_PassesNewListingsToImageDownloader()
	{
		// Arrange
		var toDownload = new List<Listing> { new() { ExternalId = "ext-1", Title = "t", Location = "l", Url = "u", ImageUrl = "i" } };
		RecordStepOrder(toDownload);
		var sut = CreateSut();

		// Act
		await sut.ProcessReportAsync(Report, Recipients, CancellationToken.None);

		// Assert
		imageDownloaderMock.Verify(x => x.DownloadImagesAsync(toDownload, It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task ProcessReportAsync_ForwardsRecipientsToNotificationService()
	{
		// Arrange
		RecordStepOrder();
		var sut = CreateSut();

		// Act
		await sut.ProcessReportAsync(Report, Recipients, CancellationToken.None);

		// Assert
		notificationServiceMock.Verify(
			x => x.SendNotificationsAsync(Report, Recipients, It.IsAny<CancellationToken>()),
			Times.Once);
	}

	[Fact]
	public async Task ProcessReportAsync_DoesNotSaveOrDownload_WhenNotificationFails()
	{
		// Arrange
		// Neuložení je záměr: další běh notifikaci zopakuje, protože inzeráty nejsou označené jako viděné.
		RecordStepOrder();
		notificationServiceMock
			.Setup(x => x.SendNotificationsAsync(It.IsAny<ScrapingReport>(), It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
			.ThrowsAsync(new InvalidOperationException("odeslání selhalo"));
		var sut = CreateSut();

		// Act & Assert
		await Assert.ThrowsAsync<InvalidOperationException>(
			() => sut.ProcessReportAsync(Report, Recipients, CancellationToken.None));

		unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
		imageDownloaderMock.Verify(
			x => x.DownloadImagesAsync(It.IsAny<List<Listing>>(), It.IsAny<CancellationToken>()),
			Times.Never);
	}

	[Fact]
	public async Task ProcessReportAsync_DoesNotNotify_WhenRemovedDetectionFails()
	{
		// Arrange
		RecordStepOrder();
		removedDetectorMock
			.Setup(x => x.DetectAsync(It.IsAny<ScrapingReport>(), It.IsAny<CancellationToken>()))
			.ThrowsAsync(new InvalidOperationException("detekce selhala"));
		var sut = CreateSut();

		// Act & Assert
		await Assert.ThrowsAsync<InvalidOperationException>(
			() => sut.ProcessReportAsync(Report, Recipients, CancellationToken.None));

		notificationServiceMock.Verify(
			x => x.SendNotificationsAsync(It.IsAny<ScrapingReport>(), It.IsAny<List<string>>(), It.IsAny<CancellationToken>()),
			Times.Never);
		unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
	}
}
