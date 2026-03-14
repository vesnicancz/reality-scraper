namespace RealityScraper.Web.Shared;

public static class TimeZoneHelper
{
	private static readonly TimeZoneInfo AppTimeZone = GetAppTimeZone();

	private static TimeZoneInfo GetAppTimeZone()
	{
		try
		{
			return TimeZoneInfo.FindSystemTimeZoneById("Europe/Prague");
		}
		catch (TimeZoneNotFoundException)
		{
			return TimeZoneInfo.Utc;
		}
	}

	public static string FormatLocal(DateTimeOffset? value, string format = "g")
	{
		return value.HasValue
				? TimeZoneInfo.ConvertTime(value.Value, AppTimeZone).ToString(format)
				: "—";
	}
}