using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RealityScraper.Application.Interfaces.Mailing;

namespace RealityScraper.Infrastructure.Utilities.Mailing;

// Služba pro odesílání e-mailů (beze změny)
public class SmtpEmailService : IEmailService
{
	private readonly IConfiguration configuration;
	private readonly ILogger<SmtpEmailService> logger;

	public SmtpEmailService(
		IConfiguration configuration,
		ILogger<SmtpEmailService> logger
		)
	{
		this.configuration = configuration;
		this.logger = logger;
	}

	public async Task SendEmailNotificationAsync(string emailBody, List<string> recipients, CancellationToken cancellationToken)
	{
		var smtpSettings = configuration.GetSection("SmtpSettings");

		if (recipients == null || !recipients.Any())
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
					Body = emailBody
				};

				foreach (var recipient in recipients)
				{
					mailMessage.To.Add(recipient);
				}

				await client.SendMailAsync(mailMessage, cancellationToken);
				logger.LogInformation("E-mail s novými nabídkami byl úspěšně odeslán.");
			}
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Chyba při odesílání e-mailu.");
		}
	}
}