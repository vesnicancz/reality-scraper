using RealityScraper.Model;

namespace RealityScraper.Mailing;

public interface IHtmlMailGenerator
{
	string GenerateHtmlBody(List<Listing> listings);
}