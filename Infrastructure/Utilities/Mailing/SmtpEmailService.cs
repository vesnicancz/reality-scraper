using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RealityScraper.Application.Interfaces.Mailing;
using RealityScraper.Infrastructure.Configuration;

namespace RealityScraper.Infrastructure.Utilities.Mailing;

// Služba pro odesílání e-mailů (beze změny)
public class SmtpEmailService : IEmailService
{
	private readonly SmtpOptions options;
	private readonly ILogger<SmtpEmailService> logger;

	public SmtpEmailService(
		IOptions<SmtpOptions> options,
		ILogger<SmtpEmailService> logger
		)
	{
		this.options = options.Value;
		this.logger = logger;
	}

	public async Task SendEmailNotificationAsync(string emailBody, List<string> recipients, CancellationToken cancellationToken)
	{
		if (recipients == null || !recipients.Any())
		{
			logger.LogWarning("Nejsou nastaveni žádní příjemci e-mailů.");
			return;
		}

		try
		{
			using (var client = new SmtpClient(options.Server)
			{
				Port = options.Port,
				Credentials = new System.Net.NetworkCredential(options.Username, options.Password),
				EnableSsl = options.EnableSsl
			})
			{
				var mailMessage = new MailMessage
				{
					From = new MailAddress(options.FromAddress, options.FromName),
					Subject = $"Nové realitní nabídky ({DateTime.Now:dd.MM.yyyy})",
					IsBodyHtml = true,
					Body = emailBody
				};

				foreach (var recipient in recipients)
				{
					mailMessage.To.Add(recipient);
				}

				await client.SendMailAsync(mailMessage, cancellationToken);
				logger.LogTrace("E-mail s novými nabídkami byl úspěšně odeslán.");
			}
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Chyba při odesílání e-mailu.");
		}
	}
}