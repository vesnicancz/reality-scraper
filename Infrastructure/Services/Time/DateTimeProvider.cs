using System.Runtime.InteropServices;
using RealityScraper.SharedKernel;

namespace RealityScraper.Infrastructure.Services.Time;

/// <summary>
/// Provides current time in local time-zone ("Central Europe Standard Time", "Europe/Prague" for non-Windows platforms).
/// </summary>
public class DateTimeProvider : IDateTimeProvider
{
	/// <summary>
	/// Returns time-zone you want to treat as local ("Central Europe Standard Time", "Europe/Prague" for non-Windows platforms).
	/// </summary>
	private static TimeZoneInfo CurrentTimeZone
	{
		get
		{
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				return TimeZoneInfo.FindSystemTimeZoneById("Central Europe Standard Time");
			}
			return TimeZoneInfo.FindSystemTimeZoneById("Europe/Prague"); // MacOS
		}
	}

	/// <summary>
	/// Vrací aktuální datum (bez času).
	/// </summary>
	public DateTimeOffset GetCurrentDate()
	{
		return GetCurrentTime().Date;
	}

	/// <summary>
	/// Vrací aktuální čas.
	/// </summary>
	public DateTimeOffset GetCurrentTime()
	{
		return TimeZoneInfo.ConvertTime(DateTimeOffset.Now, CurrentTimeZone).ToUniversalTime();
	}
}