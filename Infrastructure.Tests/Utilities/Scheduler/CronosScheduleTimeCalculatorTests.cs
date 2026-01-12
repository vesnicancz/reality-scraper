using RealityScraper.Infrastructure.Utilities.Scheduler;

namespace RealityScraper.Infrastructure.Tests.Utilities.Scheduler;

public class CronosScheduleTimeCalculatorTests
{
	[Fact]
	public void CronosScheduleTimeCalculator_GetNextExecutionTime_Foo()
	{
		// Arrange
		var currentTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Prague");
		var date = TimeZoneInfo.ConvertTime(new DateTime(2025, 01, 01, 8, 0, 0), currentTimeZone);

		var sut = new CronosScheduleTimeCalculator();

		// Act
		var result = sut.GetNextExecutionTime("0 12 * * *", date);

		// Assert
		Assert.NotNull(result);
		Assert.Equal(new DateTime(2025, 01, 01, 12, 0, 0), TimeZoneInfo.ConvertTime(result.Value, currentTimeZone));
	}
}