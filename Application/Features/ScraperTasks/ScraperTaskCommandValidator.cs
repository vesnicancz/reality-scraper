using FluentValidation;
using RealityScraper.Application.Interfaces.Scheduler;

namespace RealityScraper.Application.Features.ScraperTasks;

internal sealed class ScraperTaskCommandValidator : AbstractValidator<IScraperTaskCommand>
{
	public ScraperTaskCommandValidator(IScheduleTimeCalculator timeCalculator)
	{
		RuleFor(x => x.Name)
			.NotEmpty()
			.WithMessage("Name is required.")
			.MaximumLength(100)
			.WithMessage("Name must not exceed 100 characters.");

		RuleFor(x => x.CronExpression)
			.NotEmpty()
			.WithMessage("Cron expression is required.")
			.MaximumLength(50)
			.WithMessage("Cron expression must not exceed 50 characters.")
			.Must(timeCalculator.IsValidExpression)
			.WithMessage("Cron expression is not valid.");

		RuleForEach(x => x.Recipients)
			.SetValidator(new ScraperTaskRecipientInputValidator());

		RuleForEach(x => x.Targets)
			.SetValidator(new ScraperTaskTargetInputValidator());
	}
}