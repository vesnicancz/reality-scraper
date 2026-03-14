namespace RealityScraper.SharedKernel;

public interface IDateTimeProvider
{
	/// <summary>
	/// Returns the current time in UTC.
	/// </summary>
	DateTimeOffset UtcNow { get; }

	/// <summary>
	/// The application's display timezone.
	/// </summary>
	TimeZoneInfo ApplicationTimeZone { get; }

	/// <summary>
	/// Converts a UTC time to the application timezone for display purposes.
	/// </summary>
	DateTimeOffset ToApplicationTime(DateTimeOffset utcTime);
}