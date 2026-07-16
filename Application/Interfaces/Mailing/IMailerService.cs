using RealityScraper.Application.Features.Reporting.Model;
using RealityScraper.Application.Features.Scraping.Model.Report;

namespace RealityScraper.Application.Interfaces.Mailing;

public interface IMailerService
{
	/// <summary>
	/// Odešle report nových a cenově změněných inzerátů. Vrací true, pokud se odeslání podařilo.
	/// </summary>
	Task<bool> SendListingReportAsync(ScrapingReport scrapingReport, List<string> recipients, CancellationToken cancellationToken);

	/// <summary>
	/// Odešle report vyřazených inzerátů. Vrací true, pokud se odeslání podařilo.
	/// </summary>
	Task<bool> SendRemovedListingsReportAsync(RemovedListingsReport report, List<string> recipients, IReadOnlyList<EmailAttachmentData> attachments, CancellationToken cancellationToken);
}