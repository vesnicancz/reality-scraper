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

	public Task<bool> SendEmailNotificationAsync(string subject, string emailBody, List<string> recipients, CancellationToken cancellationToken)
	{
		return SendEmailNotificationAsync(subject, emailBody, recipients, Array.Empty<EmailAttachmentData>(), cancellationToken);
	}

	public async Task<bool> SendEmailNotificationAsync(string subject, string emailBody, List<string> recipients, IReadOnlyList<EmailAttachmentData> attachments, CancellationToken cancellationToken)
	{
		if (recipients == null || !recipients.Any())
		{
			logger.LogWarning("Nejsou nastaveni žádní příjemci e-mailů.");
			return false;
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
				using var mailMessage = new MailMessage
				{
					From = new MailAddress(options.FromAddress, options.FromName),
					Subject = subject,
					IsBodyHtml = true,
					Body = emailBody
				};

				if (attachments.Count > 0)
				{
					// Inline obrázky (cid:) vyžadují AlternateView s LinkedResources
					var htmlView = AlternateView.CreateAlternateViewFromString(emailBody, null, "text/html");
					foreach (var attachment in attachments)
					{
						var resource = new LinkedResource(new MemoryStream(attachment.Content), attachment.ContentType);
						if (attachment.ContentId != null)
						{
							resource.ContentId = attachment.ContentId;
						}
						htmlView.LinkedResources.Add(resource);
					}
					mailMessage.AlternateViews.Add(htmlView);
				}

				foreach (var recipient in recipients)
				{
					mailMessage.To.Add(recipient);
				}

				await client.SendMailAsync(mailMessage, cancellationToken);
				logger.LogTrace("E-mail úspěšně odeslán.");
				return true;
			}
		}
		catch (OperationCanceledException)
		{
			throw;
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Chyba při odesílání e-mailu.");
			return false;
		}
	}
}