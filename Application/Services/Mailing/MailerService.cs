using RealityScraper.Application.Features.Scraping.Model.Report;
using RealityScraper.Application.Interfaces.Mailing;

namespace RealityScraper.Application.Services.Mailing;

public class MailerService : IMailerService
{
	private readonly IEmailGenerator htmlMailBodyGenerator;
	private readonly IEmailService emailService;

	public MailerService(IEmailGenerator htmlMailBodyGenerator, IEmailService emailService)
	{
		this.htmlMailBodyGenerator = htmlMailBodyGenerator;
		this.emailService = emailService;
	}

	//public async Task SendEmailAsync(string to, string subject, string body)
	//{
	//	throw new NotImplementedException();
	//}

	public async Task SendNewListingsAsync(ScrapingReport scrapingReport, List<string> recipients, CancellationToken cancellationToken)
	{
		var subject = string.IsNullOrWhiteSpace(scrapingReport.TaskName)
			? $"Nové realitní nabídky ({DateTime.Now:dd.MM.yyyy})"
			: $"{scrapingReport.TaskName} – nové nabídky ({DateTime.Now:dd.MM.yyyy})";
		var emailBody = await htmlMailBodyGenerator.GenerateHtmlBodyAsync(scrapingReport, cancellationToken);
		await emailService.SendEmailNotificationAsync(subject, emailBody, recipients, cancellationToken);
	}
}