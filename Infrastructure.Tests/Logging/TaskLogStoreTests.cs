using RealityScraper.Infrastructure.Logging;

namespace RealityScraper.Infrastructure.Tests.Logging;

public class TaskLogStoreTests
{
	// Musí odpovídat MaxLines v TaskLogStore.
	private const int MaxLines = 1000;

	[Fact]
	public void GetAndClear_ReturnsNull_WhenCaptureWasNeverStarted()
	{
		// Arrange
		var sut = new TaskLogStore();

		// Act
		var result = sut.GetAndClear(Guid.NewGuid());

		// Assert
		Assert.Null(result);
	}

	[Fact]
	public void Append_IsIgnored_WhenCaptureWasNotStarted()
	{
		// Arrange
		// Logy z úloh, které nesbíráme, nesmí nekontrolovaně růst v paměti.
		var sut = new TaskLogStore();
		var taskId = Guid.NewGuid();

		// Act
		sut.Append(taskId, "řádek mimo záznam");

		// Assert
		Assert.Null(sut.GetAndClear(taskId));
	}

	[Fact]
	public void GetAndClear_ReturnsAppendedLinesInOrder()
	{
		// Arrange
		var sut = new TaskLogStore();
		var taskId = Guid.NewGuid();
		sut.StartCapture(taskId);

		// Act
		sut.Append(taskId, "první");
		sut.Append(taskId, "druhý");

		// Assert
		Assert.Equal($"první{Environment.NewLine}druhý", sut.GetAndClear(taskId));
	}

	[Fact]
	public void GetAndClear_ReturnsEmptyString_WhenNothingWasAppended()
	{
		// Arrange
		var sut = new TaskLogStore();
		var taskId = Guid.NewGuid();
		sut.StartCapture(taskId);

		// Act
		var result = sut.GetAndClear(taskId);

		// Assert
		Assert.Equal(string.Empty, result);
	}

	[Fact]
	public void GetAndClear_RemovesTheCapture()
	{
		// Arrange
		// Log se vyzvedává jednou (po doběhnutí úlohy) a nesmí zůstat viset v paměti.
		var sut = new TaskLogStore();
		var taskId = Guid.NewGuid();
		sut.StartCapture(taskId);
		sut.Append(taskId, "řádek");

		// Act
		var first = sut.GetAndClear(taskId);
		var second = sut.GetAndClear(taskId);

		// Assert
		Assert.Equal("řádek", first);
		Assert.Null(second);
	}

	[Fact]
	public void StartCapture_DiscardsPreviousContentForTheSameTask()
	{
		// Arrange
		var sut = new TaskLogStore();
		var taskId = Guid.NewGuid();
		sut.StartCapture(taskId);
		sut.Append(taskId, "z předchozího běhu");

		// Act
		sut.StartCapture(taskId);
		sut.Append(taskId, "z aktuálního běhu");

		// Assert
		Assert.Equal("z aktuálního běhu", sut.GetAndClear(taskId));
	}

	[Fact]
	public void Capture_KeepsTasksIsolated()
	{
		// Arrange
		var sut = new TaskLogStore();
		var firstTaskId = Guid.NewGuid();
		var secondTaskId = Guid.NewGuid();
		sut.StartCapture(firstTaskId);
		sut.StartCapture(secondTaskId);

		// Act
		sut.Append(firstTaskId, "první úloha");
		sut.Append(secondTaskId, "druhá úloha");

		// Assert
		Assert.Equal("první úloha", sut.GetAndClear(firstTaskId));
		Assert.Equal("druhá úloha", sut.GetAndClear(secondTaskId));
	}

	[Fact]
	public void Append_TruncatesAfterMaxLines()
	{
		// Arrange
		var sut = new TaskLogStore();
		var taskId = Guid.NewGuid();
		sut.StartCapture(taskId);

		// Act
		for (var i = 0; i < MaxLines + 50; i++)
		{
			sut.Append(taskId, $"řádek {i}");
		}

		// Assert
		var result = sut.GetAndClear(taskId);
		Assert.NotNull(result);
		var lines = result.Split(Environment.NewLine);
		Assert.Equal(MaxLines + 1, lines.Length);
		Assert.Contains("zkrácen", lines[^1]);
		Assert.DoesNotContain($"řádek {MaxLines}", result);
	}

	[Fact]
	public void Append_WritesTruncationNoticeOnlyOnce()
	{
		// Arrange
		var sut = new TaskLogStore();
		var taskId = Guid.NewGuid();
		sut.StartCapture(taskId);

		// Act
		for (var i = 0; i < MaxLines + 500; i++)
		{
			sut.Append(taskId, $"řádek {i}");
		}

		// Assert
		var result = sut.GetAndClear(taskId);
		Assert.NotNull(result);
		var noticeCount = result.Split(Environment.NewLine).Count(l => l.Contains("zkrácen"));
		Assert.Equal(1, noticeCount);
	}

	[Fact]
	public void Append_TruncatesWhenTotalSizeExceedsLimit()
	{
		// Arrange
		// Málo, ale velmi dlouhých řádků nesmí obejít limit na počet řádků.
		var sut = new TaskLogStore();
		var taskId = Guid.NewGuid();
		sut.StartCapture(taskId);
		var longLine = new string('x', 64 * 1024);

		// Act
		for (var i = 0; i < 20; i++)
		{
			sut.Append(taskId, longLine);
		}

		// Assert
		var result = sut.GetAndClear(taskId);
		Assert.NotNull(result);
		Assert.Contains("zkrácen", result);
		Assert.True(result.Length < 20 * longLine.Length);
	}
}
