namespace RealityScraper.Scraping.Model;

public record ListingItem(string Title, string Description, decimal? Price, string Location, string Url, string ImageUrl, string ExternalId);