namespace RealityScraper.Application.Interfaces.Mailing;

public interface IEmailService
{
	Task SendEmailNotificationAsync(string subject, string mailBody, List<string> recipients, CancellationToken cancellationToken);
}