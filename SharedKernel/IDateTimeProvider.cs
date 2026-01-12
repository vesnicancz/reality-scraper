namespace RealityScraper.SharedKernel;

public interface IDateTimeProvider
{
	/// <summary>
	/// Returns the current date (without time).
	/// </summary>
	DateTime GetCurrentDate();

	/// <summary>
	/// Returns the current time.
	/// </summary>
	DateTime GetCurrentTime();
}