using RealityScraper.SharedKernel;

namespace RealityScraper.Infrastructure.Services.Time;

public class DateTimeProvider : IDateTimeProvider
{
	private static readonly TimeZoneInfo PragueTimeZone =
		TimeZoneInfo.FindSystemTimeZoneById("Europe/Prague");

	public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;

	public TimeZoneInfo ApplicationTimeZone => PragueTimeZone;

	public DateTimeOffset ToApplicationTime(DateTimeOffset utcTime)
	{
		return TimeZoneInfo.ConvertTime(utcTime, PragueTimeZone);
	}
}