using RealityScraper.Model;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace RealityScraper.Mailing
{
	public class SendGridEmailService : IEmailService
	{
		private readonly ILogger<SendGridEmailService> logger;
		private readonly IConfiguration configuration;

		public SendGridEmailService(ILogger<SendGridEmailService> logger, IConfiguration configuration)
		{
			this.logger = logger;
			this.configuration = configuration;
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
				var htmlContent = BuildEmailBody(listings);

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

		private string BuildEmailBody(List<Listing> listings)
		{
			var body = new System.Text.StringBuilder();
			body.AppendLine("<!DOCTYPE html>");
			body.AppendLine("<html>");
			body.AppendLine("<head>");
			body.AppendLine("<style>");
			body.AppendLine("body { font-family: Arial, sans-serif; }");
			body.AppendLine(".listing { margin-bottom: 20px; border-bottom: 1px solid #ccc; padding-bottom: 15px; }");
			body.AppendLine(".listing img { max-width: 300px; max-height: 200px; }");
			body.AppendLine("</style>");
			body.AppendLine("</head>");
			body.AppendLine("<body>");
			body.AppendLine("<h1>Nové realitní nabídky</h1>");
			body.AppendLine($"<p>Datum: {DateTime.Now:dd.MM.yyyy HH:mm}</p>");
			body.AppendLine($"<p>Celkem nalezeno: {listings.Count} nových nabídek</p>");

			foreach (var listing in listings)
			{
				body.AppendLine("<div class='listing'>");
				body.AppendLine($"<h2>{listing.Title}</h2>");
				body.AppendLine($"<p><strong>Cena:</strong> {listing.Price?.ToString("C0")}</p>");
				body.AppendLine($"<p><strong>Lokalita:</strong> {listing.Location}</p>");

				if (!string.IsNullOrEmpty(listing.ImageUrl))
				{
					body.AppendLine($"<p><img src='{listing.ImageUrl}' alt='{listing.Title}'></p>");
				}

				body.AppendLine($"<p><a href='{listing.Url}' target='_blank'>Zobrazit detail nabídky</a></p>");
				body.AppendLine("</div>");
			}

			body.AppendLine("</body>");
			body.AppendLine("</html>");
			return body.ToString();
		}
	}
}