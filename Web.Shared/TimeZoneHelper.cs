namespace RealityScraper.Web.Shared;

public static class TimeZoneHelper
{
	private static readonly TimeZoneInfo AppTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Prague");

	public static string FormatLocal(DateTimeOffset? value, string format = "g")
	{
		return value.HasValue
				? TimeZoneInfo.ConvertTime(value.Value, AppTimeZone).ToString(format)
				: "—";
	}
}