using Microsoft.Extensions.Logging;
using RealityScraper.Application.Abstractions.Database;
using RealityScraper.Application.Features.Reporting.Model;
using RealityScraper.Application.Interfaces.Mailing;
using RealityScraper.Application.Interfaces.Repositories.Configuration;
using RealityScraper.Application.Interfaces.Repositories.Realty;
using RealityScraper.Application.Interfaces.Scheduler;
using RealityScraper.Application.Interfaces.Scraping;
using RealityScraper.SharedKernel;

namespace RealityScraper.Application.Features.Reporting;

/// <summary>
/// Plánovaná úloha odesílající report inzerátů vyřazených od posledního úspěšného reportu.
/// </summary>
public class RemovedListingsReportJob : IScheduledJob
{
	private const int MaxEmbeddedImages = 30;
	private const long MaxEmbeddedImagesBytes = 10 * 1024 * 1024;

	private readonly IReportTaskRepository reportTaskRepository;
	private readonly IListingRepository listingRepository;
	private readonly IListingImageReader listingImageReader;
	private readonly IMailerService mailerService;
	private readonly IUnitOfWork unitOfWork;
	private readonly IDateTimeProvider dateTimeProvider;
	private readonly ILogger<RemovedListingsReportJob> logger;

	public RemovedListingsReportJob(
		IReportTaskRepository reportTaskRepository,
		IListingRepository listingRepository,
		IListingImageReader listingImageReader,
		IMailerService mailerService,
		IUnitOfWork unitOfWork,
		IDateTimeProvider dateTimeProvider,
		ILogger<RemovedListingsReportJob> logger)
	{
		this.reportTaskRepository = reportTaskRepository;
		this.listingRepository = listingRepository;
		this.listingImageReader = listingImageReader;
		this.mailerService = mailerService;
		this.unitOfWork = unitOfWork;
		this.dateTimeProvider = dateTimeProvider;
		this.logger = logger;
	}

	public async Task ExecuteAsync(Guid taskId, CancellationToken cancellationToken)
	{
		var reportTask = await reportTaskRepository.GetTaskWithDetailsAsync(taskId, cancellationToken);
		if (reportTask == null)
		{
			logger.LogError("Report úloha {TaskId} nebyla nalezena.", taskId);
			return;
		}

		var now = dateTimeProvider.UtcNow;
		var from = reportTask.LastSuccessfulReportAt ?? now.AddDays(-7);
		var to = now;

		logger.LogInformation("Sestavuji report vyřazených inzerátů '{Name}' za období {From} – {To}.", reportTask.Name, from, to);

		var sections = new List<RemovedListingsTaskSection>();
		foreach (var source in reportTask.Sources)
		{
			var removedListings = await listingRepository.GetRemovedInPeriodAsync(source.ScraperTaskId, from, to, cancellationToken);
			if (removedListings.Count == 0)
			{
				continue;
			}

			sections.Add(new RemovedListingsTaskSection
			{
				ScraperTaskName = source.ScraperTask.Name,
				Listings = removedListings.Select(l => new RemovedListingItem
				{
					ListingId = l.Id,
					Title = l.Title,
					Location = l.Location,
					Price = l.Price,
					Url = l.Url,
					CreatedAt = dateTimeProvider.ToApplicationTime(l.CreatedAt),
					RemovedAt = l.RemovedAt.HasValue ? dateTimeProvider.ToApplicationTime(l.RemovedAt.Value) : null
				}).ToList()
			});
		}

		var recipients = reportTask.Recipients.Select(r => r.Email).ToList();

		if (sections.Count == 0 || recipients.Count == 0)
		{
			if (sections.Count == 0)
			{
				logger.LogInformation("Report '{Name}': v období nebyly vyřazeny žádné inzeráty, e-mail se neposílá.", reportTask.Name);
			}
			else
			{
				logger.LogWarning("Report '{Name}': nejsou nastaveni žádní příjemci, e-mail se neposílá.", reportTask.Name);
			}

			reportTask.SetLastSuccessfulReportAt(to);
			await unitOfWork.SaveChangesAsync(cancellationToken);
			return;
		}

		var report = new RemovedListingsReport
		{
			ReportName = reportTask.Name,
			ReportDate = dateTimeProvider.ToApplicationTime(now),
			PeriodFrom = dateTimeProvider.ToApplicationTime(from),
			PeriodTo = dateTimeProvider.ToApplicationTime(to),
			Sections = sections
		};

		var attachments = await BuildImageAttachmentsAsync(sections, cancellationToken);

		logger.LogInformation("Report '{Name}': {Count} vyřazených inzerátů, {ImageCount} obrázků v příloze, {RecipientCount} příjemců.",
			reportTask.Name, report.RemovedListingsCount, attachments.Count, recipients.Count);

		var sent = await mailerService.SendRemovedListingsReportAsync(report, recipients, attachments, cancellationToken);
		if (!sent)
		{
			throw new InvalidOperationException($"Odeslání reportu vyřazených inzerátů '{reportTask.Name}' se nezdařilo.");
		}

		reportTask.SetLastSuccessfulReportAt(to);
		await unitOfWork.SaveChangesAsync(cancellationToken);
	}

	/// <summary>
	/// Načte nakešované obrázky (novější vyřazení mají přednost) a označí položky,
	/// které mají obrázek v příloze. Počet i celková velikost příloh jsou omezeny.
	/// </summary>
	private async Task<List<EmailAttachmentData>> BuildImageAttachmentsAsync(List<RemovedListingsTaskSection> sections, CancellationToken cancellationToken)
	{
		var attachments = new List<EmailAttachmentData>();
		long totalBytes = 0;

		var itemsByRemovedAt = sections
			.SelectMany(s => s.Listings)
			.OrderByDescending(l => l.RemovedAt);

		foreach (var item in itemsByRemovedAt)
		{
			if (attachments.Count >= MaxEmbeddedImages)
			{
				break;
			}

			var imageBytes = await listingImageReader.TryReadImageAsync(item.ListingId, cancellationToken);
			if (imageBytes == null)
			{
				continue;
			}

			if (totalBytes + imageBytes.Length > MaxEmbeddedImagesBytes)
			{
				// Jeden velký obrázek nezastaví vkládání dalších (menších) v rámci rozpočtu;
				// tvrdým stropem zůstává jen počet příloh (MaxEmbeddedImages).
				continue;
			}

			totalBytes += imageBytes.Length;
			attachments.Add(new EmailAttachmentData($"{item.ListingId}.jpg", imageBytes, "image/jpeg", item.ListingId.ToString()));
			item.HasImage = true;
		}

		return attachments;
	}
}
