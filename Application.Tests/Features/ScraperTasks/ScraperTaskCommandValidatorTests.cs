using FluentValidation.TestHelper;
using Moq;
using RealityScraper.Application.Features.ScraperTasks;
using RealityScraper.Application.Interfaces.Scheduler;
using RealityScraper.Domain.Enums;

namespace RealityScraper.Application.Tests.Features.ScraperTasks;

public class ScraperTaskCommandValidatorTests
{
	private readonly Mock<IScheduleTimeCalculator> timeCalculatorMock = new();

	private ScraperTaskCommandValidator CreateSut(bool cronValid = true)
	{
		timeCalculatorMock.Setup(x => x.IsValidExpression(It.IsAny<string>())).Returns(cronValid);
		return new ScraperTaskCommandValidator(timeCalculatorMock.Object);
	}

	private static TestCommand ValidCommand()
	{
		return new TestCommand
		{
			Name = "Byty Praha",
			CronExpression = "0 6 * * *",
			Enabled = true,
			Recipients = [new ScraperTaskRecipientInput("user@example.com")],
			Targets = [new ScraperTaskTargetInput((int)ScrapersEnum.SReality, "https://www.sreality.cz/hledani")]
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
		var command = ValidCommand() with { Name = "" };

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
		var command = ValidCommand() with { CronExpression = "" };

		var result = sut.TestValidate(command);

		result.ShouldHaveValidationErrorFor(x => x.CronExpression);
	}

	[Fact]
	public void InvalidCron_FailsValidation()
	{
		var sut = CreateSut(cronValid: false);
		var command = ValidCommand() with { CronExpression = "definitely-not-cron" };

		var result = sut.TestValidate(command);

		result.ShouldHaveValidationErrorFor(x => x.CronExpression);
	}

	[Fact]
	public void NoTargets_FailsValidation()
	{
		var sut = CreateSut();
		var command = ValidCommand() with { Targets = [] };

		var result = sut.TestValidate(command);

		result.ShouldHaveValidationErrorFor(x => x.Targets);
	}

	[Theory]
	[InlineData("")]
	[InlineData("not a url")]
	[InlineData("ftp://example.com")]
	public void InvalidTargetUrl_FailsValidation(string url)
	{
		var sut = CreateSut();
		var command = ValidCommand() with
		{
			Targets = [new ScraperTaskTargetInput((int)ScrapersEnum.SReality, url)]
		};

		var result = sut.TestValidate(command);

		Assert.False(result.IsValid);
	}

	[Fact]
	public void InvalidScraperType_FailsValidation()
	{
		var sut = CreateSut();
		var command = ValidCommand() with
		{
			Targets = [new ScraperTaskTargetInput(9999, "https://www.sreality.cz/hledani")]
		};

		var result = sut.TestValidate(command);

		Assert.False(result.IsValid);
	}

	[Theory]
	[InlineData("")]
	[InlineData("not-an-email")]
	public void InvalidRecipientEmail_FailsValidation(string email)
	{
		var sut = CreateSut();
		var command = ValidCommand() with
		{
			Recipients = [new ScraperTaskRecipientInput(email)]
		};

		var result = sut.TestValidate(command);

		Assert.False(result.IsValid);
	}

	[Fact]
	public void NoRecipients_IsAllowed()
	{
		// Příjemci nejsou povinní (report může běžet i bez okamžité notifikace).
		var sut = CreateSut();
		var command = ValidCommand() with { Recipients = [] };

		var result = sut.TestValidate(command);

		result.ShouldNotHaveAnyValidationErrors();
	}

	private sealed record TestCommand : IScraperTaskCommand
	{
		public string Name { get; init; } = string.Empty;
		public string CronExpression { get; init; } = string.Empty;
		public bool Enabled { get; init; }
		public List<ScraperTaskRecipientInput> Recipients { get; init; } = [];
		public List<ScraperTaskTargetInput> Targets { get; init; } = [];
	}
}
