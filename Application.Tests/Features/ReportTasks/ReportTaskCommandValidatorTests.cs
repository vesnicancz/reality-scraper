using FluentValidation.TestHelper;
using Moq;
using RealityScraper.Application.Features.ReportTasks;
using RealityScraper.Application.Interfaces.Scheduler;

namespace RealityScraper.Application.Tests.Features.ReportTasks;

public class ReportTaskCommandValidatorTests
{
	private readonly Mock<IScheduleTimeCalculator> timeCalculatorMock = new();

	private ReportTaskCommandValidator CreateSut(bool cronValid = true)
	{
		timeCalculatorMock.Setup(x => x.IsValidExpression(It.IsAny<string>())).Returns(cronValid);
		return new ReportTaskCommandValidator(timeCalculatorMock.Object);
	}

	private static TestCommand ValidCommand()
	{
		return new TestCommand
		{
			Name = "Týdenní přehled vyřazených",
			CronExpression = "0 6 * * 1",
			Enabled = true,
			Recipients = [new ReportTaskRecipientInput("user@example.com")],
			ScraperTaskIds = [Guid.NewGuid()]
		};
	}

	[Fact]
	public void ValidCommand_PassesValidation()
	{
		var sut = CreateSut();

		var result = sut.TestValidate(ValidCommand());

		result.ShouldNotHaveAnyValidationErrors();
	}

	[Fact]
	public void EmptyName_FailsValidation()
	{
		var sut = CreateSut();
		var command = ValidCommand() with { Name = string.Empty };

		var result = sut.TestValidate(command);

		result.ShouldHaveValidationErrorFor(x => x.Name);
	}

	[Fact]
	public void TooLongName_FailsValidation()
	{
		var sut = CreateSut();
		var command = ValidCommand() with { Name = new string('a', 101) };

		var result = sut.TestValidate(command);

		result.ShouldHaveValidationErrorFor(x => x.Name);
	}

	[Fact]
	public void EmptyCron_FailsValidation()
	{
		var sut = CreateSut();
		var command = ValidCommand() with { CronExpression = string.Empty };

		var result = sut.TestValidate(command);

		result.ShouldHaveValidationErrorFor(x => x.CronExpression);
	}

	[Fact]
	public void InvalidCron_FailsValidation()
	{
		var sut = CreateSut(cronValid: false);
		var command = ValidCommand() with { CronExpression = "nesmysl" };

		var result = sut.TestValidate(command);

		result.ShouldHaveValidationErrorFor(x => x.CronExpression);
	}

	[Fact]
	public void TooLongCron_FailsValidation()
	{
		var sut = CreateSut();
		var command = ValidCommand() with { CronExpression = new string('*', 51) };

		var result = sut.TestValidate(command);

		result.ShouldHaveValidationErrorFor(x => x.CronExpression);
	}

	[Theory]
	[InlineData("")]
	[InlineData("neplatny-email")]
	public void InvalidRecipientEmail_FailsValidation(string email)
	{
		var sut = CreateSut();
		var command = ValidCommand() with { Recipients = [new ReportTaskRecipientInput(email)] };

		var result = sut.TestValidate(command);

		Assert.False(result.IsValid);
	}

	[Fact]
	public void TooLongRecipientEmail_FailsValidation()
	{
		var sut = CreateSut();
		var email = new string('a', 95) + "@example.com";
		var command = ValidCommand() with { Recipients = [new ReportTaskRecipientInput(email)] };

		var result = sut.TestValidate(command);

		Assert.False(result.IsValid);
	}

	[Fact]
	public void EmptyScraperTaskId_FailsValidation()
	{
		var sut = CreateSut();
		var command = ValidCommand() with { ScraperTaskIds = [Guid.Empty] };

		var result = sut.TestValidate(command);

		Assert.False(result.IsValid);
	}

	[Fact]
	public void NoRecipients_IsAllowed()
	{
		// Příjemci nejsou povinní - report může existovat i bez okamžité notifikace.
		var sut = CreateSut();
		var command = ValidCommand() with { Recipients = [] };

		var result = sut.TestValidate(command);

		result.ShouldNotHaveAnyValidationErrors();
	}

	[Fact]
	public void NoScraperTaskIds_IsAllowed()
	{
		// Zdroje lze doplnit později, samotný prázdný seznam validaci neshodí.
		var sut = CreateSut();
		var command = ValidCommand() with { ScraperTaskIds = [] };

		var result = sut.TestValidate(command);

		result.ShouldNotHaveAnyValidationErrors();
	}

	private sealed record TestCommand : IReportTaskCommand
	{
		public string Name { get; init; } = string.Empty;
		public string CronExpression { get; init; } = string.Empty;
		public bool Enabled { get; init; }
		public List<ReportTaskRecipientInput> Recipients { get; init; } = [];
		public List<Guid> ScraperTaskIds { get; init; } = [];
	}
}
