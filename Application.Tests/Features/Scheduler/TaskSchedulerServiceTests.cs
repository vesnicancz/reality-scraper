using Microsoft.Extensions.Logging;
using Moq;
using RealityScraper.Application.Abstractions.Database;
using RealityScraper.Application.Features.Scheduler;
using RealityScraper.Application.Interfaces.Repositories.Configuration;
using RealityScraper.Application.Interfaces.Scheduler;
using RealityScraper.Domain.Entities.Tasks;
using RealityScraper.Domain.Enums;
using RealityScraper.SharedKernel;

namespace RealityScraper.Application.Tests.Features.Scheduler;

public class TaskSchedulerServiceTests
{
	private static readonly DateTimeOffset Now = new(2026, 7, 15, 12, 0, 0, TimeSpan.Zero);
	private static readonly DateTimeOffset CalculatedNextRun = Now.AddHours(6);

	private readonly Mock<ITaskRepository> taskRepositoryMock = new();
	private readonly Mock<IUnitOfWork> unitOfWorkMock = new();
	private readonly Mock<IScheduleTimeCalculator> timeCalculatorMock = new();
	private readonly Mock<IDateTimeProvider> dateTimeProviderMock = new();

	private TaskSchedulerService CreateSut(bool cronValid = true)
	{
		dateTimeProviderMock.Setup(x => x.UtcNow).Returns(Now);
		timeCalculatorMock.Setup(x => x.IsValidExpression(It.IsAny<string>())).Returns(cronValid);
		timeCalculatorMock
			.Setup(x => x.GetNextExecutionTime(It.IsAny<string>(), It.IsAny<DateTimeOffset>()))
			.Returns(CalculatedNextRun);

		return new TaskSchedulerService(
			taskRepositoryMock.Object,
			unitOfWorkMock.Object,
			timeCalculatorMock.Object,
			dateTimeProviderMock.Object,
			Mock.Of<ILogger<TaskSchedulerService>>());
	}

	private void SetupActiveTasks(params TaskBase[] tasks)
	{
		taskRepositoryMock
			.Setup(x => x.GetActiveTasksAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(tasks.ToList());
	}

	private static ScraperTask CreateScraperTask(string cron = "0 6 * * *", DateTimeOffset? nextRunAt = null)
	{
		return new ScraperTask("Byty Brno", cron, true, Now.AddDays(-1), nextRunAt);
	}

	private static RemovedListingsReportTask CreateReportTask(string cron = "0 7 * * 1")
	{
		return new RemovedListingsReportTask("Přehled vyřazených", cron, true, Now.AddDays(-1), Now.AddDays(1));
	}

	[Fact]
	public async Task LoadActiveTasksAsync_MapsScraperTaskToScraperType()
	{
		// Arrange
		var task = CreateScraperTask(nextRunAt: Now.AddHours(2));
		task.SetLastRunAt(Now.AddHours(-22));
		SetupActiveTasks(task);
		var sut = CreateSut();

		// Act
		var result = await sut.LoadActiveTasksAsync(CancellationToken.None);

		// Assert
		var info = Assert.Single(result);
		Assert.Equal(task.Id, info.Id);
		Assert.Equal("Byty Brno", info.Name);
		Assert.Equal("0 6 * * *", info.CronExpression);
		Assert.Equal(ScheduledTaskType.Scraper, info.TaskType);
		Assert.Equal(Now.AddHours(2), info.NextRunTime);
		Assert.Equal(Now.AddHours(-22), info.LastRunTime);
		Assert.False(info.IsRunning);
	}

	[Fact]
	public async Task LoadActiveTasksAsync_MapsRemovedListingsReportTaskToReportType()
	{
		// Arrange
		SetupActiveTasks(CreateReportTask());
		var sut = CreateSut();

		// Act
		var result = await sut.LoadActiveTasksAsync(CancellationToken.None);

		// Assert
		Assert.Equal(ScheduledTaskType.RemovedListingsReport, Assert.Single(result).TaskType);
	}

	[Fact]
	public async Task LoadActiveTasksAsync_SkipsTasksWithInvalidCronExpression()
	{
		// Arrange
		// Neplatný cron nesmí shodit načtení ostatních úloh.
		var invalid = CreateScraperTask("nesmysl");
		var valid = CreateScraperTask();
		SetupActiveTasks(invalid, valid);
		var sut = CreateSut();
		timeCalculatorMock.Setup(x => x.IsValidExpression("nesmysl")).Returns(false);

		// Act
		var result = await sut.LoadActiveTasksAsync(CancellationToken.None);

		// Assert
		Assert.Equal(valid.Id, Assert.Single(result).Id);
	}

	[Fact]
	public async Task LoadActiveTasksAsync_CalculatesNextRunTime_WhenTaskHasNone()
	{
		// Arrange
		SetupActiveTasks(CreateScraperTask(nextRunAt: null));
		var sut = CreateSut();

		// Act
		var result = await sut.LoadActiveTasksAsync(CancellationToken.None);

		// Assert
		Assert.Equal(CalculatedNextRun, Assert.Single(result).NextRunTime);
		timeCalculatorMock.Verify(x => x.GetNextExecutionTime("0 6 * * *", Now), Times.Once);
	}

	[Fact]
	public async Task LoadActiveTasksAsync_KeepsStoredNextRunTime_WhenTaskHasOne()
	{
		// Arrange
		// Uložený čas má přednost - jinak by "spustit teď" (NextRunAt = teď) přepočet zahodil.
		var storedNextRun = Now.AddMinutes(-1);
		SetupActiveTasks(CreateScraperTask(nextRunAt: storedNextRun));
		var sut = CreateSut();

		// Act
		var result = await sut.LoadActiveTasksAsync(CancellationToken.None);

		// Assert
		Assert.Equal(storedNextRun, Assert.Single(result).NextRunTime);
		timeCalculatorMock.Verify(
			x => x.GetNextExecutionTime(It.IsAny<string>(), It.IsAny<DateTimeOffset>()),
			Times.Never);
	}

	[Fact]
	public async Task LoadActiveTasksAsync_ReturnsEmptyList_WhenThereAreNoActiveTasks()
	{
		// Arrange
		SetupActiveTasks();
		var sut = CreateSut();

		// Act
		var result = await sut.LoadActiveTasksAsync(CancellationToken.None);

		// Assert
		Assert.Empty(result);
	}

	[Fact]
	public async Task LoadActiveTasksAsync_Throws_ForUnknownTaskType()
	{
		// Arrange
		// Nový potomek TaskBase musí být doplněn do mapování, jinak by se tiše neplánoval.
		SetupActiveTasks(new UnknownTask("Neznámá", "0 6 * * *", true, Now, null));
		var sut = CreateSut();

		// Act & Assert
		await Assert.ThrowsAsync<NotSupportedException>(() => sut.LoadActiveTasksAsync(CancellationToken.None));
	}

	[Fact]
	public async Task CalculateNextRunTimeAsync_DelegatesToTimeCalculator()
	{
		// Arrange
		var sut = CreateSut();

		// Act
		var result = await sut.CalculateNextRunTimeAsync("0 6 * * *", Now, CancellationToken.None);

		// Assert
		Assert.Equal(CalculatedNextRun, result);
		timeCalculatorMock.Verify(x => x.GetNextExecutionTime("0 6 * * *", Now), Times.Once);
	}

	[Fact]
	public async Task UpdateTaskExecutionTimesAsync_PersistsResult()
	{
		// Arrange
		var taskId = Guid.NewGuid();
		var executionResult = new TaskExecutionResult(Now, CalculatedNextRun, "hotovo", true);
		var sut = CreateSut();

		// Act
		await sut.UpdateTaskExecutionTimesAsync(taskId, executionResult, CancellationToken.None);

		// Assert
		taskRepositoryMock.Verify(
			x => x.UpdateTaskExecutionResultAsync(taskId, executionResult, It.IsAny<CancellationToken>()),
			Times.Once);
		unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
	}

	private sealed class UnknownTask : TaskBase
	{
		public UnknownTask(string name, string cronExpression, bool enabled, DateTimeOffset createdAt, DateTimeOffset? nextRunAt)
		{
			Name = name;
			CronExpression = cronExpression;
			Enabled = enabled;
			CreatedAt = createdAt;
			NextRunAt = nextRunAt;
		}
	}
}
