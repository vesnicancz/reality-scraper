using System.Globalization;

namespace RealityScraper.Application.Features.Scraping.Scrapers;

/// <summary>
/// Parsování ceny ze scrapovaného textu. Nečíselné hodnoty
/// ("Info o ceně u RK", "Cena na vyžádání") vrací jako null.
/// </summary>
public static class PriceParser
{
	public static decimal? ParseNullablePrice(string? value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return null;
		}

		// Portály používají jako oddělovač tisíců i pevnou ( ) nebo úzkou ( )
		// mezeru - char.IsWhiteSpace je pokrývá všechny.
		var cleaned = string.Concat(
			value.Replace("Kč", string.Empty).Where(c => !char.IsWhiteSpace(c)));

		if (decimal.TryParse(cleaned, NumberStyles.None, CultureInfo.InvariantCulture, out var result))
		{
			return result;
		}

		return null;
	}
}
