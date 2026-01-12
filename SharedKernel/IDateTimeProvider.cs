namespace RealityScraper.SharedKernel;

public interface IDateTimeProvider
{
	/// <summary>
	/// Returns the current date (without time).
	/// </summary>
	DateTimeOffset GetCurrentDate();

	/// <summary>
	/// Returns the current time.
	/// </summary>
	DateTimeOffset GetCurrentTime();
}