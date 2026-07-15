using RealityScraper.Application.Features.Scraping.Scrapers;

namespace RealityScraper.Application.Tests.Features.Scraping.Scrapers;

public class PriceParserTests
{
	[Theory]
	[InlineData("4 500 000 Kč", 4500000)]
	[InlineData("4500000", 4500000)]
	[InlineData("  15 000 Kč ", 15000)]
	public void ParseNullablePrice_ValidPrice_ReturnsValue(string input, decimal expected)
	{
		Assert.Equal(expected, PriceParser.ParseNullablePrice(input));
	}

	[Theory]
	[InlineData(' ')] // pevná mezera
	[InlineData(' ')] // úzká pevná mezera
	[InlineData(' ')] // tenká mezera
	public void ParseNullablePrice_NonBreakingSpaceSeparators_ReturnsValue(char separator)
	{
		var input = $"4{separator}500{separator}000 Kč";

		Assert.Equal(4500000m, PriceParser.ParseNullablePrice(input));
	}

	[Theory]
	[InlineData("Info o ceně u RK")]
	[InlineData("Cena na vyžádání")]
	[InlineData("15 000 Kč/měsíc")]
	[InlineData("")]
	[InlineData("   ")]
	[InlineData(null)]
	public void ParseNullablePrice_NonNumericValue_ReturnsNull(string? input)
	{
		Assert.Null(PriceParser.ParseNullablePrice(input));
	}
}
