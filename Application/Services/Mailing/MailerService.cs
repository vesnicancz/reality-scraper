using RealityScraper.Application.Features.Reporting.Model;
using RealityScraper.Application.Features.Scraping.Model.Report;
using RealityScraper.Application.Interfaces.Mailing;

namespace RealityScraper.Application.Services.Mailing;

public class MailerService : IMailerService
{
	private readonly IEmailGenerator emailGenerator;
	private readonly IEmailService emailService;

	public MailerService(IEmailGenerator emailGenerator, IEmailService emailService)
	{
		this.emailGenerator = emailGenerator;
		this.emailService = emailService;
	}

	public async Task<bool> SendListingReportAsync(ScrapingReport scrapingReport, List<string> recipients, CancellationToken cancellationToken)
	{
		var date = scrapingReport.ReportDate.ToString("dd.MM.yyyy");
		var subject = string.IsNullOrWhiteSpace(scrapingReport.TaskName)
			? $"Nové realitní nabídky ({date})"
			: $"{scrapingReport.TaskName} – nové nabídky ({date})";
		var emailBody = await emailGenerator.GenerateHtmlBodyAsync(scrapingReport, cancellationToken);
		return await emailService.SendEmailNotificationAsync(subject, emailBody, recipients, cancellationToken);
	}

	public async Task<bool> SendRemovedListingsReportAsync(RemovedListingsReport report, List<string> recipients, IReadOnlyList<EmailAttachmentData> attachments, CancellationToken cancellationToken)
	{
		var period = $"{report.PeriodFrom:dd.MM.}–{report.PeriodTo:dd.MM.yyyy}";
		var subject = string.IsNullOrWhiteSpace(report.ReportName)
			? $"Zrušené nabídky ({period})"
			: $"{report.ReportName} – zrušené nabídky ({period})";
		var emailBody = await emailGenerator.GenerateRemovedListingsHtmlAsync(report, cancellationToken);
		return await emailService.SendEmailNotificationAsync(subject, emailBody, recipients, attachments, cancellationToken);
	}
}