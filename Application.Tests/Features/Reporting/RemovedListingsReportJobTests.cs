using Microsoft.Extensions.Logging;
using Moq;
using RealityScraper.Application.Abstractions.Database;
using RealityScraper.Application.Features.Reporting;
using RealityScraper.Application.Features.Reporting.Model;
using RealityScraper.Application.Interfaces.Mailing;
using RealityScraper.Application.Interfaces.Repositories.Configuration;
using RealityScraper.Application.Interfaces.Repositories.Realty;
using RealityScraper.Application.Interfaces.Scraping;
using RealityScraper.Domain.Entities.Realty;
using RealityScraper.Domain.Entities.Tasks;
using RealityScraper.SharedKernel;

namespace RealityScraper.Application.Tests.Features.Reporting;

public class RemovedListingsReportJobTests
{
	private static readonly DateTimeOffset Now = new(2026, 7, 15, 6, 0, 0, TimeSpan.Zero);

	private readonly Mock<IReportTaskRepository> reportTaskRepositoryMock = new();
	private readonly Mock<IListingRepository> listingRepositoryMock = new();
	private readonly Mock<IListingImageReader> listingImageReaderMock = new();
	private readonly Mock<IMailerService> mailerServiceMock = new();
	private readonly Mock<IUnitOfWork> unitOfWorkMock = new();
	private readonly Mock<IDateTimeProvider> dateTimeProviderMock = new();

	private RemovedListingsReportJob CreateSut()
	{
		dateTimeProviderMock.Setup(x => x.UtcNow).Returns(Now);
		dateTimeProviderMock.Setup(x => x.ToApplicationTime(It.IsAny<DateTimeOffset>())).Returns<DateTimeOffset>(d => d);

		return new RemovedListingsReportJob(
			reportTaskRepositoryMock.Object,
			listingRepositoryMock.Object,
			listingImageReaderMock.Object,
			mailerServiceMock.Object,
			unitOfWorkMock.Object,
			dateTimeProviderMock.Object,
			Mock.Of<ILogger<RemovedListingsReportJob>>());
	}

	private RemovedListingsReportTask CreateReportTask(ScraperTask scraperTask, DateTimeOffset? lastSuccessfulReportAt = null, string? recipientEmail = "user@example.com")
	{
		var reportTask = new RemovedListingsReportTask("report", "0 6 * * 0", true, Now.AddDays(-30), null)
		{
			Id = Guid.NewGuid()
		};

		if (lastSuccessfulReportAt != null)
		{
			reportTask.SetLastSuccessfulReportAt(lastSuccessfulReportAt.Value);
		}

		if (recipientEmail != null)
		{
			reportTask.AddRecipient(new ReportTaskRecipient(recipientEmail));
		}

		reportTask.AddSource(new ReportTaskSource(scraperTask));

		reportTaskRepositoryMock
			.Setup(x => x.GetTaskWithDetailsAsync(reportTask.Id, It.IsAny<CancellationToken>()))
			.ReturnsAsync(reportTask);

		return reportTask;
	}

	private static ScraperTask CreateScraperTask(string name = "scraper")
	{
		return new ScraperTask(name, "0 * * * *", true, Now.AddDays(-60), null)
		{
			Id = Guid.NewGuid()
		};
	}

	private static Listing CreateRemovedListing(DateTimeOffset removedAt)
	{
		return new Listing
		{
			Id = Guid.NewGuid(),
			ExternalId = Guid.NewGuid().ToString(),
			Title = "Title",
			Location = "Location",
			Url = "Url",
			ImageUrl = string.Empty,
			CreatedAt = removedAt.AddDays(-10),
			LastSeenAt = removedAt,
			RemovedAt = removedAt
		};
	}

	[Fact]
	public async Task ExecuteAsync_PeriodStartsAtLastSuccessfulReport()
	{
		// arrange
		var anchor = Now.AddDays(-10);
		var scraperTask = CreateScraperTask();
		var reportTask = CreateReportTask(scraperTask, lastSuccessfulReportAt: anchor);

		listingRepositoryMock
			.Setup(x => x.GetRemovedInPeriodAsync(scraperTask.Id, anchor, Now, It.IsAny<CancellationToken>()))
			.ReturnsAsync([]);

		var sut = CreateSut();

		// act
		await sut.ExecuteAsync(reportTask.Id, CancellationToken.None);

		// assert
		listingRepositoryMock.Verify(x => x.GetRemovedInPeriodAsync(scraperTask.Id, anchor, Now, It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task ExecuteAsync_FirstRunUsesSevenDayWindow()
	{
		// arrange
		var scraperTask = CreateScraperTask();
		var reportTask = CreateReportTask(scraperTask);

		listingRepositoryMock
			.Setup(x => x.GetRemovedInPeriodAsync(scraperTask.Id, Now.AddDays(-7), Now, It.IsAny<CancellationToken>()))
			.ReturnsAsync([]);

		var sut = CreateSut();

		// act
		await sut.ExecuteAsync(reportTask.Id, CancellationToken.None);

		// assert
		listingRepositoryMock.Verify(x => x.GetRemovedInPeriodAsync(scraperTask.Id, Now.AddDays(-7), Now, It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task ExecuteAsync_NoRemovedListings_NoEmailAndAnchorAdvanced()
	{
		// arrange
		var scraperTask = CreateScraperTask();
		var reportTask = CreateReportTask(scraperTask);

		listingRepositoryMock
			.Setup(x => x.GetRemovedInPeriodAsync(It.IsAny<Guid>(), It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync([]);

		var sut = CreateSut();

		// act
		await sut.ExecuteAsync(reportTask.Id, CancellationToken.None);

		// assert
		mailerServiceMock.Verify(
			x => x.SendRemovedListingsReportAsync(It.IsAny<RemovedListingsReport>(), It.IsAny<List<string>>(), It.IsAny<IReadOnlyList<EmailAttachmentData>>(), It.IsAny<CancellationToken>()),
			Times.Never);
		Assert.Equal(Now, reportTask.LastSuccessfulReportAt);
		unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task ExecuteAsync_SuccessfulSend_AnchorAdvanced()
	{
		// arrange
		var scraperTask = CreateScraperTask();
		var reportTask = CreateReportTask(scraperTask);

		listingRepositoryMock
			.Setup(x => x.GetRemovedInPeriodAsync(It.IsAny<Guid>(), It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync([CreateRemovedListing(Now.AddDays(-1))]);

		listingImageReaderMock
			.Setup(x => x.TryReadImageAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync((byte[]?)null);

		mailerServiceMock
			.Setup(x => x.SendRemovedListingsReportAsync(It.IsAny<RemovedListingsReport>(), It.IsAny<List<string>>(), It.IsAny<IReadOnlyList<EmailAttachmentData>>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(true);

		var sut = CreateSut();

		// act
		await sut.ExecuteAsync(reportTask.Id, CancellationToken.None);

		// assert
		mailerServiceMock.Verify(
			x => x.SendRemovedListingsReportAsync(
				It.Is<RemovedListingsReport>(r => r.RemovedListingsCount == 1 && r.Sections.Single().ScraperTaskName == "scraper"),
				It.Is<List<string>>(r => r.Single() == "user@example.com"),
				It.IsAny<IReadOnlyList<EmailAttachmentData>>(),
				It.IsAny<CancellationToken>()),
			Times.Once);
		Assert.Equal(Now, reportTask.LastSuccessfulReportAt);
	}

	[Fact]
	public async Task ExecuteAsync_FailedSend_ThrowsAndAnchorNotAdvanced()
	{
		// arrange
		var scraperTask = CreateScraperTask();
		var anchor = Now.AddDays(-10);
		var reportTask = CreateReportTask(scraperTask, lastSuccessfulReportAt: anchor);

		listingRepositoryMock
			.Setup(x => x.GetRemovedInPeriodAsync(It.IsAny<Guid>(), It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync([CreateRemovedListing(Now.AddDays(-1))]);

		listingImageReaderMock
			.Setup(x => x.TryReadImageAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync((byte[]?)null);

		mailerServiceMock
			.Setup(x => x.SendRemovedListingsReportAsync(It.IsAny<RemovedListingsReport>(), It.IsAny<List<string>>(), It.IsAny<IReadOnlyList<EmailAttachmentData>>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(false);

		var sut = CreateSut();

		// act + assert
		await Assert.ThrowsAsync<InvalidOperationException>(() => sut.ExecuteAsync(reportTask.Id, CancellationToken.None));

		Assert.Equal(anchor, reportTask.LastSuccessfulReportAt);
		unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
	}

	[Fact]
	public async Task ExecuteAsync_CachedImage_IsAttachedAndItemMarked()
	{
		// arrange
		var scraperTask = CreateScraperTask();
		var reportTask = CreateReportTask(scraperTask);
		var withImage = CreateRemovedListing(Now.AddDays(-1));
		var withoutImage = CreateRemovedListing(Now.AddDays(-2));

		listingRepositoryMock
			.Setup(x => x.GetRemovedInPeriodAsync(It.IsAny<Guid>(), It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync([withImage, withoutImage]);

		listingImageReaderMock
			.Setup(x => x.TryReadImageAsync(withImage.Id, It.IsAny<CancellationToken>()))
			.ReturnsAsync([1, 2, 3]);
		listingImageReaderMock
			.Setup(x => x.TryReadImageAsync(withoutImage.Id, It.IsAny<CancellationToken>()))
			.ReturnsAsync((byte[]?)null);

		RemovedListingsReport? sentReport = null;
		IReadOnlyList<EmailAttachmentData>? sentAttachments = null;
		mailerServiceMock
			.Setup(x => x.SendRemovedListingsReportAsync(It.IsAny<RemovedListingsReport>(), It.IsAny<List<string>>(), It.IsAny<IReadOnlyList<EmailAttachmentData>>(), It.IsAny<CancellationToken>()))
			.Callback<RemovedListingsReport, List<string>, IReadOnlyList<EmailAttachmentData>, CancellationToken>((r, _, a, _) =>
			{
				sentReport = r;
				sentAttachments = a;
			})
			.ReturnsAsync(true);

		var sut = CreateSut();

		// act
		await sut.ExecuteAsync(reportTask.Id, CancellationToken.None);

		// assert
		Assert.NotNull(sentReport);
		Assert.NotNull(sentAttachments);

		var attachment = Assert.Single(sentAttachments);
		Assert.Equal(withImage.Id.ToString(), attachment.ContentId);
		Assert.Equal("image/jpeg", attachment.ContentType);

		var items = sentReport.Sections.Single().Listings;
		Assert.True(items.Single(i => i.ListingId == withImage.Id).HasImage);
		Assert.False(items.Single(i => i.ListingId == withoutImage.Id).HasImage);
	}

	[Fact]
	public async Task ExecuteAsync_NoRecipients_NoEmailAndAnchorAdvanced()
	{
		// arrange
		var scraperTask = CreateScraperTask();
		var reportTask = CreateReportTask(scraperTask, recipientEmail: null);

		listingRepositoryMock
			.Setup(x => x.GetRemovedInPeriodAsync(It.IsAny<Guid>(), It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync([CreateRemovedListing(Now.AddDays(-1))]);

		var sut = CreateSut();

		// act
		await sut.ExecuteAsync(reportTask.Id, CancellationToken.None);

		// assert
		mailerServiceMock.Verify(
			x => x.SendRemovedListingsReportAsync(It.IsAny<RemovedListingsReport>(), It.IsAny<List<string>>(), It.IsAny<IReadOnlyList<EmailAttachmentData>>(), It.IsAny<CancellationToken>()),
			Times.Never);
		Assert.Equal(Now, reportTask.LastSuccessfulReportAt);
	}
}
