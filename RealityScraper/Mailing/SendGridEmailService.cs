using RealityScraper.Model;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace RealityScraper.Mailing
{
	public class SendGridEmailService : IEmailService
	{
		private readonly ILogger<SendGridEmailService> logger;
		private readonly IConfiguration configuration;
		private readonly IHtmlMailGenerator htmlMailBodyGenerator;

		public SendGridEmailService(
			ILogger<SendGridEmailService> logger,
			IConfiguration configuration,
			IHtmlMailGenerator htmlMailBodyGenerator)
		{
			this.logger = logger;
			this.configuration = configuration;
			this.htmlMailBodyGenerator = htmlMailBodyGenerator;
		}

		public async Task SendEmailNotificationAsync(List<Listing> listings)
		{
			var apiKey = configuration["SendGridSettings:ApiKey"];
			var fromEmail = configuration["SendGridSettings:FromEmail"];
			var fromName = configuration["SendGridSettings:FromName"];
			var recipientEmails = configuration.GetSection("EmailSettings:Recipients").Get<List<string>>();

			if (string.IsNullOrEmpty(apiKey))
			{
				logger.LogError("SendGrid API key is not configured");
				return;
			}

			if (recipientEmails == null || recipientEmails.Count == 0)
			{
				logger.LogWarning("Nejsou nastaveni žádní příjemci e-mailů.");
				return;
			}

			try
			{
				var client = new SendGridClient(apiKey);
				var from = new EmailAddress(fromEmail, fromName);
				var subject = $"Nové realitní nabídky ({DateTime.Now:dd.MM.yyyy})";
				var htmlContent = htmlMailBodyGenerator.GenerateHtmlBody(listings);

				// Create a message for each recipient (or use BCC for multiple recipients)
				foreach (var recipientEmail in recipientEmails)
				{
					var to = new EmailAddress(recipientEmail);
					var msg = MailHelper.CreateSingleEmail(from, to, subject, "", htmlContent);
					var response = await client.SendEmailAsync(msg);

					if (response.StatusCode == System.Net.HttpStatusCode.Accepted ||
						response.StatusCode == System.Net.HttpStatusCode.OK)
					{
						logger.LogInformation($"Email sent successfully to {recipientEmail}");
					}
					else
					{
						logger.LogWarning($"Failed to send email to {recipientEmail}: {response.StatusCode}");
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