using RealityScraper.Scraping.Model;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace RealityScraper.Mailing
{
	public class SendGridEmailService : IEmailService
	{
		private readonly IConfiguration configuration;
		private readonly IEmailGenerator htmlMailBodyGenerator;
		private readonly ILogger<SendGridEmailService> logger;

		public SendGridEmailService(
			IConfiguration configuration,
			IEmailGenerator htmlMailBodyGenerator,
			ILogger<SendGridEmailService> logger
			)
		{
			this.configuration = configuration;
			this.htmlMailBodyGenerator = htmlMailBodyGenerator;
			this.logger = logger;
		}

		public async Task SendEmailNotificationAsync(ScrapingReport scrapingReport, List<string> recipients)
		{
			var apiKey = configuration["SendGridSettings:ApiKey"];
			var fromEmail = configuration["SendGridSettings:FromEmail"];
			var fromName = configuration["SendGridSettings:FromName"];

			if (string.IsNullOrEmpty(apiKey))
			{
				logger.LogError("SendGrid API key is not configured");
				return;
			}

			if (recipients == null || recipients.Count == 0)
			{
				logger.LogWarning("Nejsou nastaveni žádní příjemci e-mailů.");
				return;
			}

			try
			{
				var client = new SendGridClient(apiKey);
				var from = new EmailAddress(fromEmail, fromName);
				var subject = $"Nové realitní nabídky ({DateTime.Now:dd.MM.yyyy})";
				var htmlContent = await htmlMailBodyGenerator.GenerateHtmlBodyAsync(scrapingReport);

				// Create a message for each recipient (or use BCC for multiple recipients)
				foreach (var recipientEmail in recipients)
				{
					var to = new EmailAddress(recipientEmail);
					var msg = MailHelper.CreateSingleEmail(from, to, subject, "", htmlContent);
					var response = await client.SendEmailAsync(msg);

					if (response.StatusCode == System.Net.HttpStatusCode.Accepted ||
						response.StatusCode == System.Net.HttpStatusCode.OK)
					{
						logger.LogInformation("Email sent successfully to {recipientEmail}", recipientEmail);
					}
					else
					{
						logger.LogWarning("Failed to send email to {recipientEmail}: {statusCode}", recipientEmail, response.StatusCode);
					}
				}

				logger.LogInformation("E-mail s novými nabídkami byl úspěšně odeslán.");
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Chyba při odesílání e-mailu přes SendGrid.");
			}
		}
	}
}