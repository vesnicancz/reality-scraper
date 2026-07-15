using Microsoft.Extensions.Logging.Abstractions;
using RazorEngineCore;
using RealityScraper.Application.Features.Reporting.Model;
using RealityScraper.Infrastructure.Utilities.Mailing;

namespace RealityScraper.Infrastructure.Tests.Utilities.Mailing;

public class RazorEmailGeneratorTests
{
	[Fact]
	public async Task GenerateRemovedListingsHtmlAsync_RendersTemplate()
	{
		// arrange
		var listingWithImage = new RemovedListingItem
		{
			ListingId = Guid.NewGuid(),
			Title = "Prodej domu",
			Location = "Vesnice u Brna",
			Price = 4_500_000,
			Url = "https://example.com/inzerat/1",
			CreatedAt = new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero),
			RemovedAt = new DateTimeOffset(2026, 7, 12, 0, 0, 0, TimeSpan.Zero),
			HasImage = true
		};

		var listingWithoutImage = new RemovedListingItem
		{
			ListingId = Guid.NewGuid(),
			Title = "Prodej bytu",
			Location = "Praha",
			Price = null,
			Url = "https://example.com/inzerat/2",
			CreatedAt = new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero),
			RemovedAt = new DateTimeOffset(2026, 7, 10, 0, 0, 0, TimeSpan.Zero),
			HasImage = false
		};

		var report = new RemovedListingsReport
		{
			ReportName = "Můj report",
			ReportDate = new DateTimeOffset(2026, 7, 13, 6, 0, 0, TimeSpan.Zero),
			PeriodFrom = new DateTimeOffset(2026, 7, 6, 6, 0, 0, TimeSpan.Zero),
			PeriodTo = new DateTimeOffset(2026, 7, 13, 6, 0, 0, TimeSpan.Zero),
			Sections =
			[
				new RemovedListingsTaskSection
				{
					ScraperTaskName = "Domy u Brna",
					Listings = [listingWithImage, listingWithoutImage]
				}
			]
		};

		var sut = new RazorEmailGenerator(new RazorEngine(), NullLogger<RazorEmailGenerator>.Instance);

		// act
		var html = await sut.GenerateRemovedListingsHtmlAsync(report, CancellationToken.None);

		// assert
		Assert.Contains("Domy u Brna", html);
		Assert.Contains($"cid:{listingWithImage.ListingId}", html);
		Assert.DoesNotContain($"cid:{listingWithoutImage.ListingId}", html);
		Assert.Contains("Vesnice u Brna", html);
		Assert.Contains("12.07.2026", html);
	}
}
