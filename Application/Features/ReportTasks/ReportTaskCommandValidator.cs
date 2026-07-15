using FluentValidation;
using RealityScraper.Application.Interfaces.Scheduler;

namespace RealityScraper.Application.Features.ReportTasks;

internal sealed class ReportTaskCommandValidator : AbstractValidator<IReportTaskCommand>
{
	public ReportTaskCommandValidator(IScheduleTimeCalculator timeCalculator)
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
			.SetValidator(new ReportTaskRecipientInputValidator());

		RuleForEach(x => x.ScraperTaskIds)
			.NotEmpty()
			.WithMessage("Scraper task ID must not be empty.");
	}
}
