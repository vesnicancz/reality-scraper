using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RealityScraper.Application.Diagnostics;
using RealityScraper.Application.Interfaces.Mailing;
using RealityScraper.Infrastructure.Configuration;
using Resend;

namespace RealityScraper.Infrastructure.Utilities.Mailing;

public class ResendEmailService : IEmailService
{
	private readonly ResendOptions options;
	private readonly IResend resend;
	private readonly ILogger<ResendEmailService> logger;

	public ResendEmailService(
		IResend resend,
		IOptions<ResendOptions> options,
		ILogger<ResendEmailService> logger
		)
	{
		this.options = options.Value;
		this.resend = resend;
		this.logger = logger;
	}

	public Task<bool> SendEmailNotificationAsync(string subject, string mailBody, List<string> recipients, CancellationToken cancellationToken)
	{
		return SendEmailNotificationAsync(subject, mailBody, recipients, Array.Empty<EmailAttachmentData>(), cancellationToken);
	}

	public async Task<bool> SendEmailNotificationAsync(string subject, string mailBody, List<string> recipients, IReadOnlyList<EmailAttachmentData> attachments, CancellationToken cancellationToken)
	{
		if (recipients == null || recipients.Count == 0)
		{
			logger.LogWarning("Nejsou nastaveni žádní příjemci e-mailů.");
			return false;
		}

		var allSucceeded = true;

		// Adresy příjemců do trace schválně nedáváme (osobní údaje), stačí jejich počet.
		using var activity = ScraperTelemetry.ActivitySource.StartActivity("Send email");
		activity?.SetTag(ScraperTelemetry.Tags.RecipientCount, recipients.Count);
		activity?.SetTag("email.attachment_count", attachments.Count);

		try
		{
			var fromAddress = !string.IsNullOrWhiteSpace(options.FromName) ? $"{options.FromName} <{options.FromEmail}>" : options.FromEmail;

			// Create a message for each recipient (or use BCC for multiple recipients)
			foreach (var recipientEmail in recipients)
			{
				var message = new EmailMessage
				{
					From = fromAddress,
					Subject = subject,
					HtmlBody = mailBody
				};

				foreach (var attachment in attachments)
				{
					message.Attachments ??= new List<EmailAttachment>();
					message.Attachments.Add(new EmailAttachment
					{
						Filename = attachment.FileName,
						Content = attachment.Content,
						ContentType = attachment.ContentType,
						ContentId = attachment.ContentId
					});
				}

				message.To.Add(recipientEmail);
				var response = await resend.EmailSendAsync(message, cancellationToken);
				if (response.Success)
				{
					logger.LogTrace("E-mail úspěšně odeslán na {RecipientEmail}.", recipientEmail);
					ScraperTelemetry.EmailsSent.Add(1, new KeyValuePair<string, object?>(ScraperTelemetry.Tags.Outcome, ScraperTelemetry.Outcomes.Success));
				}
				else
				{
					logger.LogWarning(response.Exception, "Nepodařilo se odeslat e-mail na {RecipientEmail}.", recipientEmail);
					ScraperTelemetry.EmailsSent.Add(1, new KeyValuePair<string, object?>(ScraperTelemetry.Tags.Outcome, ScraperTelemetry.Outcomes.Failure));
					activity.SetError(response.Exception, "Odeslání e-mailu selhalo");
					allSucceeded = false;
				}
			}
		}
		catch (OperationCanceledException)
		{
			throw;
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Chyba při odesílání e-mailu přes Resend.");
			activity.SetError(ex);
			allSucceeded = false;
		}

		return allSucceeded;
	}
}
