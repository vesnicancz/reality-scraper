namespace RealityScraper.Application.Interfaces.Mailing;

public interface IEmailService
{
	Task SendEmailNotificationAsync(string mailBody, List<string> recipients, CancellationToken cancellationToken);
}