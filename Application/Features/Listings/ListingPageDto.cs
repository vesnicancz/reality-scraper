namespace RealityScraper.Application.Features.Listings;

public class ListingPageDto
{
	public List<ListingDto> Items { get; set; } = [];

	public int TotalCount { get; set; }
}
