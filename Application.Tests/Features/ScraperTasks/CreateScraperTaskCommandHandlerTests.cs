using Moq;
using RealityScraper.Application.Abstractions.Database;
using RealityScraper.Application.Features.ScraperTasks;
using RealityScraper.Application.Features.ScraperTasks.Create;
using RealityScraper.Application.Interfaces.Repositories.Configuration;
using RealityScraper.Application.Interfaces.Scheduler;
using RealityScraper.Domain.Entities.Tasks;
using RealityScraper.Domain.Enums;
using RealityScraper.Domain.Events;
using RealityScraper.SharedKernel;

namespace RealityScraper.Application.Tests.Features.ScraperTasks;

public class CreateScraperTaskCommandHandlerTests
{
	private static readonly DateTimeOffset Now = new(2026, 7, 15, 12, 0, 0, TimeSpan.Zero);
	private static readonly DateTimeOffset NextRun = Now.AddHours(6);

	private readonly Mock<IScraperTaskRepository> repositoryMock = new();
	private readonly Mock<IDateTimeProvider> dateTimeProviderMock = new();
	private readonly Mock<IScheduleTimeCalculator> timeCalculatorMock = new();
	private readonly Mock<IUnitOfWork> unitOfWorkMock = new();

	private CreateScraperTaskCommandHandler CreateSut()
	{
		dateTimeProviderMock.Setup(x => x.UtcNow).Returns(Now);
		timeCalculatorMock
			.Setup(x => x.GetNextExecutionTime(It.IsAny<string>(), It.IsAny<DateTimeOffset>()))
			.Returns(NextRun);

		return new CreateScraperTaskCommandHandler(
			repositoryMock.Object,
			dateTimeProviderMock.Object,
			timeCalculatorMock.Object,
			unitOfWorkMock.Object);
	}

	private static CreateScraperTaskCommand ValidCommand(bool enabled = true)
	{
		return new CreateScraperTaskCommand(
			"Byty Brno",
			"0 6 * * *",
			enabled,
			[new ScraperTaskRecipientInput("user@example.com")],
			[new ScraperTaskTargetInput((int)ScrapersEnum.SReality, "https://www.sreality.cz/hledani")]);
	}

	[Fact]
	public async Task Handle_CreatesTaskWithRecipientsAndTargets()
	{
		// Arrange
		ScraperTask? added = null;
		repositoryMock.Setup(x => x.Add(It.IsAny<ScraperTask>())).Callback((ScraperTask t) => added = t);
		var sut = CreateSut();

		// Act
		var result = await sut.Handle(ValidCommand(), CancellationToken.None);

		// Assert
		Assert.True(result.IsSuccess);
		Assert.NotNull(added);
		Assert.Equal("Byty Brno", added.Name);
		Assert.Equal("0 6 * * *", added.CronExpression);
		Assert.True(added.Enabled);
		Assert.Equal(Now, added.CreatedAt);
		Assert.Equal("user@example.com", Assert.Single(added.Recipients).Email);
		var target = Assert.Single(added.Targets);
		Assert.Equal(ScrapersEnum.SReality, target.ScraperType);
		Assert.Equal("https://www.sreality.cz/hledani", target.Url);
		unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task Handle_SchedulesNextRun_WhenTaskIsEnabled()
	{
		// Arrange
		ScraperTask? added = null;
		repositoryMock.Setup(x => x.Add(It.IsAny<ScraperTask>())).Callback((ScraperTask t) => added = t);
		var sut = CreateSut();

		// Act
		var result = await sut.Handle(ValidCommand(enabled: true), CancellationToken.None);

		// Assert
		Assert.Equal(NextRun, added!.NextRunAt);
		Assert.Equal(NextRun, result.Value.NextRunAt);
		timeCalculatorMock.Verify(x => x.GetNextExecutionTime("0 6 * * *", Now), Times.Once);
	}

	[Fact]
	public async Task Handle_DoesNotScheduleNextRun_WhenTaskIsDisabled()
	{
		// Arrange
		// Vypnutá úloha nemá mít naplánovaný běh - jinak by ji scheduler po zapnutí spustil pozdě.
		ScraperTask? added = null;
		repositoryMock.Setup(x => x.Add(It.IsAny<ScraperTask>())).Callback((ScraperTask t) => added = t);
		var sut = CreateSut();

		// Act
		await sut.Handle(ValidCommand(enabled: false), CancellationToken.None);

		// Assert
		Assert.Null(added!.NextRunAt);
		timeCalculatorMock.Verify(
			x => x.GetNextExecutionTime(It.IsAny<string>(), It.IsAny<DateTimeOffset>()),
			Times.Never);
	}

	[Fact]
	public async Task Handle_RaisesCreatedEvent()
	{
		// Arrange
		ScraperTask? added = null;
		repositoryMock.Setup(x => x.Add(It.IsAny<ScraperTask>())).Callback((ScraperTask t) => added = t);
		var sut = CreateSut();

		// Act
		await sut.Handle(ValidCommand(), CancellationToken.None);

		// Assert
		var domainEvent = Assert.IsType<ScraperTaskCreatedEvent>(Assert.Single(added!.DomainEvents));
		Assert.Equal(added.Id, domainEvent.ScraperTaskId);
	}

	[Fact]
	public async Task Handle_ReturnsDetailDtoWithRecipientsAndTargets()
	{
		// Arrange
		var sut = CreateSut();

		// Act
		var result = await sut.Handle(ValidCommand(), CancellationToken.None);

		// Assert
		Assert.True(result.IsSuccess);
		Assert.Equal("Byty Brno", result.Value.Name);
		Assert.Equal("user@example.com", Assert.Single(result.Value.Recipients).Email);
		Assert.Equal((int)ScrapersEnum.SReality, Assert.Single(result.Value.Targets).ScraperType);
	}

	[Fact]
	public async Task Handle_CreatesTaskWithoutRecipientsOrTargets()
	{
		// Arrange
		ScraperTask? added = null;
		repositoryMock.Setup(x => x.Add(It.IsAny<ScraperTask>())).Callback((ScraperTask t) => added = t);
		var sut = CreateSut();
		var command = ValidCommand() with { Recipients = [], Targets = [] };

		// Act
		var result = await sut.Handle(command, CancellationToken.None);

		// Assert
		Assert.True(result.IsSuccess);
		Assert.Empty(added!.Recipients);
		Assert.Empty(added.Targets);
	}
}
