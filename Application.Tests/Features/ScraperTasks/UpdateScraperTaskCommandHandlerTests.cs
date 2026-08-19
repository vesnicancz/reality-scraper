using Moq;
using RealityScraper.Application.Abstractions.Database;
using RealityScraper.Application.Features.ScraperTasks;
using RealityScraper.Application.Features.ScraperTasks.Update;
using RealityScraper.Application.Interfaces.Repositories.Configuration;
using RealityScraper.Application.Interfaces.Scheduler;
using RealityScraper.Domain.Entities.Tasks;
using RealityScraper.Domain.Enums;
using RealityScraper.Domain.Events;
using RealityScraper.SharedKernel;

namespace RealityScraper.Application.Tests.Features.ScraperTasks;

public class UpdateScraperTaskCommandHandlerTests
{
	private static readonly DateTimeOffset Now = new(2026, 7, 15, 12, 0, 0, TimeSpan.Zero);
	private static readonly DateTimeOffset OriginalNextRun = Now.AddHours(2);
	private static readonly DateTimeOffset RecalculatedNextRun = Now.AddHours(6);

	private readonly Mock<IScraperTaskRepository> repositoryMock = new();
	private readonly Mock<IUnitOfWork> unitOfWorkMock = new();
	private readonly Mock<IScheduleTimeCalculator> timeCalculatorMock = new();
	private readonly Mock<IDateTimeProvider> dateTimeProviderMock = new();

	private UpdateScraperTaskCommandHandler CreateSut()
	{
		dateTimeProviderMock.Setup(x => x.UtcNow).Returns(Now);
		timeCalculatorMock
			.Setup(x => x.GetNextExecutionTime(It.IsAny<string>(), It.IsAny<DateTimeOffset>()))
			.Returns(RecalculatedNextRun);

		return new UpdateScraperTaskCommandHandler(
			repositoryMock.Object,
			unitOfWorkMock.Object,
			timeCalculatorMock.Object,
			dateTimeProviderMock.Object);
	}

	private ScraperTask SetupExistingTask(bool enabled = true, string cron = "0 6 * * *")
	{
		var task = new ScraperTask("Byty Brno", cron, enabled, Now.AddDays(-7), OriginalNextRun);
		task.AddRecipient(new ScraperTaskRecipient("old@example.com"));
		task.AddTarget(new ScraperTaskTarget(ScrapersEnum.SReality, "https://www.sreality.cz/puvodni"));

		repositoryMock
			.Setup(x => x.GetTaskWithDetailsAsync(task.Id, It.IsAny<CancellationToken>()))
			.ReturnsAsync(task);

		return task;
	}

	private static UpdateScraperTaskCommand Command(
		Guid id,
		string name = "Byty Brno",
		string cron = "0 6 * * *",
		bool enabled = true,
		List<ScraperTaskRecipientInput>? recipients = null,
		List<ScraperTaskTargetInput>? targets = null)
	{
		return new UpdateScraperTaskCommand(
			id,
			name,
			cron,
			enabled,
			recipients ?? [new ScraperTaskRecipientInput("new@example.com")],
			targets ?? [new ScraperTaskTargetInput((int)ScrapersEnum.RealityIdnes, "https://reality.idnes.cz/nove")]);
	}

	[Fact]
	public async Task Handle_ReturnsNotFound_WhenTaskDoesNotExist()
	{
		// Arrange
		repositoryMock
			.Setup(x => x.GetTaskWithDetailsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync((ScraperTask?)null);
		var sut = CreateSut();

		// Act
		var result = await sut.Handle(Command(Guid.NewGuid()), CancellationToken.None);

		// Assert
		Assert.True(result.IsFailure);
		Assert.Equal("ScraperTask.NotFound", result.Error.Code);
		unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
	}

	[Fact]
	public async Task Handle_UpdatesScalarProperties()
	{
		// Arrange
		var task = SetupExistingTask();
		var sut = CreateSut();

		// Act
		var result = await sut.Handle(Command(task.Id, name: "Domy Brno", cron: "0 8 * * *", enabled: false), CancellationToken.None);

		// Assert
		Assert.True(result.IsSuccess);
		Assert.Equal("Domy Brno", task.Name);
		Assert.Equal("0 8 * * *", task.CronExpression);
		Assert.False(task.Enabled);
		unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task Handle_ReplacesRecipientsAndTargets()
	{
		// Arrange
		var task = SetupExistingTask();
		var sut = CreateSut();

		// Act
		await sut.Handle(Command(task.Id), CancellationToken.None);

		// Assert
		Assert.Equal("new@example.com", Assert.Single(task.Recipients).Email);
		var target = Assert.Single(task.Targets);
		Assert.Equal(ScrapersEnum.RealityIdnes, target.ScraperType);
		Assert.Equal("https://reality.idnes.cz/nove", target.Url);
	}

	[Fact]
	public async Task Handle_ClearsRecipientsAndTargets_WhenCommandHasNone()
	{
		// Arrange
		var task = SetupExistingTask();
		var sut = CreateSut();

		// Act
		await sut.Handle(Command(task.Id, recipients: [], targets: []), CancellationToken.None);

		// Assert
		Assert.Empty(task.Recipients);
		Assert.Empty(task.Targets);
	}

	[Fact]
	public async Task Handle_KeepsNextRunAt_WhenNeitherCronNorEnabledChanged()
	{
		// Arrange
		// Přepočet by posunul nejbližší běh - přejmenování úlohy nesmí zdržet už naplánovaný scrap.
		var task = SetupExistingTask(enabled: true, cron: "0 6 * * *");
		var sut = CreateSut();

		// Act
		await sut.Handle(Command(task.Id, name: "Jiný název", cron: "0 6 * * *", enabled: true), CancellationToken.None);

		// Assert
		Assert.Equal(OriginalNextRun, task.NextRunAt);
		timeCalculatorMock.Verify(
			x => x.GetNextExecutionTime(It.IsAny<string>(), It.IsAny<DateTimeOffset>()),
			Times.Never);
	}

	[Fact]
	public async Task Handle_RecalculatesNextRunAt_WhenCronChanged()
	{
		// Arrange
		var task = SetupExistingTask(cron: "0 6 * * *");
		var sut = CreateSut();

		// Act
		await sut.Handle(Command(task.Id, cron: "0 18 * * *"), CancellationToken.None);

		// Assert
		Assert.Equal(RecalculatedNextRun, task.NextRunAt);
		timeCalculatorMock.Verify(x => x.GetNextExecutionTime("0 18 * * *", Now), Times.Once);
	}

	[Fact]
	public async Task Handle_ClearsNextRunAt_WhenTaskGetsDisabled()
	{
		// Arrange
		var task = SetupExistingTask(enabled: true);
		var sut = CreateSut();

		// Act
		await sut.Handle(Command(task.Id, enabled: false), CancellationToken.None);

		// Assert
		Assert.Null(task.NextRunAt);
		timeCalculatorMock.Verify(
			x => x.GetNextExecutionTime(It.IsAny<string>(), It.IsAny<DateTimeOffset>()),
			Times.Never);
	}

	[Fact]
	public async Task Handle_SchedulesNextRunAt_WhenTaskGetsEnabled()
	{
		// Arrange
		var task = SetupExistingTask(enabled: false);
		var sut = CreateSut();

		// Act
		await sut.Handle(Command(task.Id, enabled: true), CancellationToken.None);

		// Assert
		Assert.Equal(RecalculatedNextRun, task.NextRunAt);
	}

	[Fact]
	public async Task Handle_RaisesUpdatedEvent()
	{
		// Arrange
		var task = SetupExistingTask();
		var sut = CreateSut();

		// Act
		await sut.Handle(Command(task.Id), CancellationToken.None);

		// Assert
		var domainEvent = Assert.IsType<ScraperTaskUpdatedEvent>(Assert.Single(task.DomainEvents));
		Assert.Equal(task.Id, domainEvent.ScraperTaskId);
	}

	[Fact]
	public async Task Handle_ReturnsDetailDtoWithReplacedCollections()
	{
		// Arrange
		var task = SetupExistingTask();
		var sut = CreateSut();

		// Act
		var result = await sut.Handle(Command(task.Id), CancellationToken.None);

		// Assert
		Assert.True(result.IsSuccess);
		Assert.Equal(task.Id, result.Value.Id);
		Assert.Equal("new@example.com", Assert.Single(result.Value.Recipients).Email);
		Assert.Equal("https://reality.idnes.cz/nove", Assert.Single(result.Value.Targets).Url);
	}
}
