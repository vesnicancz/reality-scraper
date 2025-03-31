using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RealityScraper.Application.Interfaces.Mailing;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace RealityScraper.Infrastructure.Utilities.Mailing;

public class SendGridEmailService : IEmailService
{
	private readonly IConfiguration configuration;
	private readonly ILogger<SendGridEmailService> logger;

	public SendGridEmailService(
		IConfiguration configuration,
		IEmailGenerator htmlMailBodyGenerator,
		ILogger<SendGridEmailService> logger
		)
	{
		this.configuration = configuration;
		this.logger = logger;
	}

	public async Task SendEmailNotificationAsync(string mailBody, List<string> recipients, CancellationToken cancellationToken)
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

			// Create a message for each recipient (or use BCC for multiple recipients)
			foreach (var recipientEmail in recipients)
			{
				var to = new EmailAddress(recipientEmail);
				var msg = MailHelper.CreateSingleEmail(from, to, subject, "", mailBody);
				var response = await client.SendEmailAsync(msg, cancellationToken);

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