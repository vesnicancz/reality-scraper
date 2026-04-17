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

	public async Task SendListingReportAsync(ScrapingReport scrapingReport, List<string> recipients, CancellationToken cancellationToken)
	{
		var date = scrapingReport.ReportDate.ToString("dd.MM.yyyy");
		var subject = string.IsNullOrWhiteSpace(scrapingReport.TaskName)
			? $"Nové realitní nabídky ({date})"
			: $"{scrapingReport.TaskName} – nové nabídky ({date})";
		var emailBody = await emailGenerator.GenerateHtmlBodyAsync(scrapingReport, cancellationToken);
		await emailService.SendEmailNotificationAsync(subject, emailBody, recipients, cancellationToken);
	}
}