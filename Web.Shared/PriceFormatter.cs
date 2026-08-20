using System.Globalization;

namespace RealityScraper.Web.Shared;

/// <summary>
/// Jednotné formátování cen inzerátů pro UI – české oddělovače tisíců, bez desetinných míst.
/// </summary>
public static class PriceFormatter
{
	private static readonly CultureInfo CzechCulture = CultureInfo.GetCultureInfo("cs-CZ");

	public static string FormatPrice(decimal? price)
	{
		return price.HasValue
			? string.Create(CzechCulture, $"{price.Value:N0} Kč")
			: "—";
	}

	public static string FormatSignedPrice(decimal difference)
	{
		var sign = difference > 0 ? "+" : "−";
		return string.Create(CzechCulture, $"{sign}{Math.Abs(difference):N0} Kč");
	}

	public static string FormatSignedPercent(decimal percent)
	{
		var sign = percent > 0 ? "+" : "−";
		return string.Create(CzechCulture, $"{sign}{Math.Abs(percent):N1} %");
	}

	public static string FormatCount(int count)
	{
		return count.ToString("N0", CzechCulture);
	}

	public static string FormatSignedCount(int count)
	{
		var sign = count > 0 ? "+" : "−";
		return string.Create(CzechCulture, $"{sign}{Math.Abs(count):N0}");
	}
}