using RealityScraper.Model;

namespace RealityScraper.Mailing;
public interface IEmailService
{
	Task SendEmailNotificationAsync(List<Listing> listings);
}