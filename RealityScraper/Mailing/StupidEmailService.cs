using System.Net.Mail;
using RealityScraper.Model;

namespace RealityScraper.Mailing;

// Služba pro odesílání e-mailů (beze změny)
public class StupidEmailService : IEmailService
{
	private readonly ILogger<StupidEmailService> logger;
	private readonly IConfiguration configuration;

	public StupidEmailService(ILogger<StupidEmailService> logger, IConfiguration configuration)
	{
		this.logger = logger;
		this.configuration = configuration;
	}

	public async Task SendEmailNotificationAsync(List<Listing> listings)
	{
		var smtpSettings = configuration.GetSection("SmtpSettings");
		var recipientEmails = configuration.GetSection("EmailSettings:Recipients").Get<List<string>>();

		if (recipientEmails == null || !recipientEmails.Any())
		{
			logger.LogWarning("Nejsou nastaveni žádní příjemci e-mailů.");
			return;
		}

		try
		{
			using (var client = new SmtpClient(smtpSettings["Server"])
			{
				Port = int.Parse(smtpSettings["Port"]),
				Credentials = new System.Net.NetworkCredential(smtpSettings["Username"], smtpSettings["Password"]),
				EnableSsl = bool.Parse(smtpSettings["EnableSsl"])
			})
			{
				var mailMessage = new MailMessage
				{
					From = new MailAddress(smtpSettings["FromAddress"], smtpSettings["FromName"]),
					Subject = $"Nové realitní nabídky ({DateTime.Now:dd.MM.yyyy})",
					IsBodyHtml = true,
					Body = BuildEmailBody(listings)
				};

				foreach (var recipient in recipientEmails)
				{
					mailMessage.To.Add(recipient);
				}

				await client.SendMailAsync(mailMessage);
				logger.LogInformation("E-mail s novými nabídkami byl úspěšně odeslán.");
			}
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Chyba při odesílání e-mailu.");
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