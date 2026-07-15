using Microsoft.Extensions.Logging.Abstractions;
using RazorEngineCore;
using RealityScraper.Application.Features.Reporting.Model;
using RealityScraper.Application.Features.Scraping.Model.Report;
using RealityScraper.Infrastructure.Utilities.Mailing;

namespace RealityScraper.Infrastructure.Tests.Utilities.Mailing;

public class RazorEmailGeneratorTests
{
	private const string PlaceholderImageUri = "data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='240' height='180'><rect width='100%25' height='100%25' fill='%23cbd5e1'/></svg>";

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
		Assert.Contains("Prodej domu", html);
		Assert.Contains("Vesnice u Brna", html);
		Assert.Contains("12.07.2026", html);
		Assert.Contains("Můj report", html);
		Assert.Contains("sledováno 41 dní", html);
		Assert.Contains("sledováno 70 dní", html);
		Assert.Contains("Již nedostupné", html);
		Assert.Contains("property-image-placeholder", html);

		WritePreviewFile("preview-RemovedListingsReport.html", html.Replace($"cid:{listingWithImage.ListingId}", PlaceholderImageUri));
	}

	[Fact]
	public async Task GenerateHtmlBodyAsync_RendersTemplate()
	{
		// arrange
		var newListing = new ListingItem
		{
			Title = "Prodej domu 4+1",
			Location = "Vesnice u Brna",
			Price = 4_500_000,
			Url = "https://example.com/inzerat/1",
			ImageUrl = "https://example.com/obrazek/1.jpg",
			ExternalId = "ext-1"
		};

		var priceChangedListing = new ListingItemWithNewPrice
		{
			Title = "Prodej bytu 2+kk",
			Location = "Brno",
			Price = 2_900_000,
			OldPrice = 3_100_000,
			Url = "https://example.com/inzerat/2",
			ImageUrl = string.Empty,
			ExternalId = "ext-2"
		};

		var report = new ScrapingReport
		{
			ReportDate = new DateTimeOffset(2026, 7, 13, 6, 0, 0, TimeSpan.Zero),
			TaskName = "Domy okres Brno",
			ScrapingSucceeded = true,
			Results =
			[
				new PortalReport
				{
					SiteName = "Sreality",
					TotalListingsCount = 12,
					NewListings = [newListing],
					PriceChangedListings = [priceChangedListing]
				}
			]
		};

		var sut = new RazorEmailGenerator(new RazorEngine(), NullLogger<RazorEmailGenerator>.Instance);

		// act
		var html = await sut.GenerateHtmlBodyAsync(report, CancellationToken.None);

		// assert
		Assert.Contains("Domy okres Brno", html);
		Assert.Contains("Sreality", html);
		Assert.Contains("Prodej domu 4+1", html);
		Assert.Contains("Vesnice u Brna", html);
		Assert.Contains("Prodej bytu 2+kk", html);
		Assert.Contains(newListing.ImageUrl, html);
		Assert.Contains("&#8595;", html); // šipka dolů u poklesu ceny
		Assert.Contains("(-6,5 %)", html);
		Assert.Contains("původně", html);
		Assert.Contains("property-image-placeholder", html); // změněná nabídka bez obrázku

		WritePreviewFile("preview-ListingReport.html", html.Replace(newListing.ImageUrl, PlaceholderImageUri));
	}

	[Fact]
	public void Templates_SharedStylesAreInSync()
	{
		// arrange
		var listingReport = ReadTemplateFile("ListingReport.cshtml");
		var removedListingsReport = ReadTemplateFile("RemovedListingsReport.cshtml");

		// act
		var listingReportStyles = ExtractSharedStyles(listingReport);
		var removedListingsReportStyles = ExtractSharedStyles(removedListingsReport);

		// assert
		Assert.Equal(listingReportStyles, removedListingsReportStyles);
	}

	private static string ReadTemplateFile(string fileName)
	{
		var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Utilities", "Mailing", "Templates", fileName);
		return File.ReadAllText(path);
	}

	private static string ExtractSharedStyles(string templateContent)
	{
		const string startToken = "/* SHARED EMAIL STYLES";
		const string endToken = "/* END SHARED EMAIL STYLES */";

		var start = templateContent.IndexOf(startToken, StringComparison.Ordinal);
		Assert.True(start >= 0, $"Šablona neobsahuje značku '{startToken}'.");

		var startLineEnd = templateContent.IndexOf('\n', start);
		var end = templateContent.IndexOf(endToken, StringComparison.Ordinal);
		Assert.True(end > startLineEnd, $"Šablona neobsahuje značku '{endToken}'.");

		return templateContent.Substring(startLineEnd + 1, end - startLineEnd - 1);
	}

	private static void WritePreviewFile(string fileName, string html)
	{
		File.WriteAllText(Path.Combine(AppContext.BaseDirectory, fileName), html);
	}
}
