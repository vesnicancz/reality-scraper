namespace RealityScraper.Application.Interfaces.Mailing;

public interface IEmailService
{
	Task SendEmailNotificationAsync(string subject, string mailBody, List<string> recipients, CancellationToken cancellationToken);

	/// <summary>
	/// Odešle e-mail s přílohami. Vrací true, pokud se odeslání podařilo všem příjemcům.
	/// </summary>
	Task<bool> SendEmailNotificationAsync(string subject, string mailBody, List<string> recipients, IReadOnlyList<EmailAttachmentData> attachments, CancellationToken cancellationToken);
}