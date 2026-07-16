using RealityScraper.Infrastructure.Services.Time;
using RealityScraper.Infrastructure.Utilities.Scheduler;

namespace RealityScraper.Infrastructure.Tests.Utilities.Scheduler;

public class CronosScheduleTimeCalculatorTests
{
	private static CronosScheduleTimeCalculator CreateSut()
	{
		// Reálný DateTimeProvider -> časové pásmo Europe/Prague (jako v produkci).
		return new CronosScheduleTimeCalculator(new DateTimeProvider());
	}

	[Theory]
	[InlineData("")]
	[InlineData("   ")]
	[InlineData("not a cron")]
	[InlineData("99 99 99 99 99")]
	public void GetNextExecutionTime_InvalidExpression_ReturnsNull(string cron)
	{
		var sut = CreateSut();

		var result = sut.GetNextExecutionTime(cron, DateTimeOffset.UtcNow);

		Assert.Null(result);
	}

	[Theory]
	[InlineData("0 12 * * *", true)]
	[InlineData("*/15 * * * *", true)]
	[InlineData("", false)]
	[InlineData("   ", false)]
	[InlineData("bogus", false)]
	public void IsValidExpression_ValidatesExpression(string cron, bool expected)
	{
		var sut = CreateSut();

		Assert.Equal(expected, sut.IsValidExpression(cron));
	}

	[Fact]
	public void GetNextExecutionTime_SummerTime_ConvertsPragueNoonToUtc()
	{
		var sut = CreateSut();
		// 15. 6. 2026 platí letní čas (CEST = UTC+2), takže 12:00 v Praze = 10:00 UTC.
		var from = new DateTimeOffset(2026, 6, 15, 0, 0, 0, TimeSpan.Zero);

		var result = sut.GetNextExecutionTime("0 12 * * *", from);

		Assert.Equal(new DateTimeOffset(2026, 6, 15, 10, 0, 0, TimeSpan.Zero), result);
		Assert.Equal(TimeSpan.Zero, result!.Value.Offset);
	}

	[Fact]
	public void GetNextExecutionTime_WinterTime_ConvertsPragueNoonToUtc()
	{
		var sut = CreateSut();
		// 15. 1. 2026 platí zimní čas (CET = UTC+1), takže 12:00 v Praze = 11:00 UTC.
		var from = new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero);

		var result = sut.GetNextExecutionTime("0 12 * * *", from);

		Assert.Equal(new DateTimeOffset(2026, 1, 15, 11, 0, 0, TimeSpan.Zero), result);
	}

	[Fact]
	public void GetNextExecutionTime_SpringForwardSkippedHour_ReturnsValidFutureUtc()
	{
		var sut = CreateSut();
		// 29. 3. 2026 se v Praze posouvá 02:00 -> 03:00, takže lokální 02:30 neexistuje.
		var from = new DateTimeOffset(2026, 3, 29, 0, 0, 0, TimeSpan.Zero);

		var result = sut.GetNextExecutionTime("30 2 * * *", from);

		Assert.NotNull(result);
		Assert.True(result!.Value > from, "Další spuštění musí být v budoucnosti.");
		Assert.Equal(TimeSpan.Zero, result.Value.Offset);
	}

	[Fact]
	public void GetNextExecutionTime_FallBackAmbiguousHour_ReturnsValidFutureUtc()
	{
		var sut = CreateSut();
		// 25. 10. 2026 se v Praze posouvá 03:00 -> 02:00, takže lokální 02:30 nastává dvakrát.
		var from = new DateTimeOffset(2026, 10, 25, 0, 0, 0, TimeSpan.Zero);

		var result = sut.GetNextExecutionTime("30 2 * * *", from);

		Assert.NotNull(result);
		Assert.True(result!.Value > from, "Další spuštění musí být v budoucnosti.");
		Assert.Equal(TimeSpan.Zero, result.Value.Offset);
	}

	[Fact]
	public void GetNextExecutionTime_DailyCronAcrossSpringForward_IsMonotonic()
	{
		var sut = CreateSut();
		var current = new DateTimeOffset(2026, 3, 27, 0, 0, 0, TimeSpan.Zero);

		// Několik po sobě jdoucích spuštění přes hranici letního času musí být rostoucí a nenulová.
		DateTimeOffset? previous = null;
		for (var i = 0; i < 5; i++)
		{
			var next = sut.GetNextExecutionTime("0 8 * * *", current);
			Assert.NotNull(next);
			if (previous != null)
			{
				Assert.True(next!.Value > previous.Value, "Spuštění musí být striktně rostoucí.");
			}

			previous = next;
			current = next!.Value;
		}
	}
}
