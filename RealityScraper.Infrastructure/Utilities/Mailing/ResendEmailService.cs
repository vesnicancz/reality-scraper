using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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

	public async Task SendEmailNotificationAsync(string mailBody, List<string> recipients, CancellationToken cancellationToken)
	{
		if (recipients == null || recipients.Count == 0)
		{
			logger.LogWarning("No email recipients are set.");
			return;
		}

		try
		{
			var subject = $"Nové realitní nabídky ({DateTime.Now:dd.MM.yyyy})";
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

				message.To.Add(recipientEmail);
				var response = await resend.EmailSendAsync(message, cancellationToken);
				if (response.Success)
				{
					logger.LogTrace("Email sent successfully to {recipientEmail}", recipientEmail);
				}
				else
				{
					logger.LogWarning("Failed to send email to {recipientEmail}: {exception}", recipientEmail, response.Exception);
				}
			}
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Error sending email via Resend.");
		}
	}
}