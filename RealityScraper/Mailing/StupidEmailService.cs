using System.Net.Mail;
using RealityScraper.Model;

namespace RealityScraper.Mailing;

// Služba pro odesílání e-mailů (beze změny)
public class StupidEmailService : IEmailService
{
	private readonly ILogger<StupidEmailService> logger;
	private readonly IConfiguration configuration;
	private readonly IHtmlMailGenerator htmlMailBodyGenerator;

	public StupidEmailService(
		ILogger<StupidEmailService> logger,
		IConfiguration configuration,
		IHtmlMailGenerator htmlMailBodyGenerator)
	{
		this.logger = logger;
		this.configuration = configuration;
		this.htmlMailBodyGenerator = htmlMailBodyGenerator;
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
					Body = htmlMailBodyGenerator.GenerateHtmlBody(listings)
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
}