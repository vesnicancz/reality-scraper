using FluentValidation;
using RealityScraper.Application.Interfaces.Scheduler;
using RealityScraper.Domain.Enums;

namespace RealityScraper.Application.Features.ScraperTasks.Update;

internal sealed class UpdateScraperTaskCommandValidator : AbstractValidator<UpdateScraperTaskCommand>
{
	public UpdateScraperTaskCommandValidator(IScheduleTimeCalculator timeCalculator)
	{
		RuleFor(x => x.Id)
			.NotEmpty()
			.WithMessage("ScraperTask ID is required.");

		RuleFor(x => x.Name)
			.NotEmpty()
			.WithMessage("Name is required.");

		RuleFor(x => x.CronExpression)
			.NotEmpty()
			.WithMessage("Cron expression is required.")
			.Must(timeCalculator.IsValidExpression)
			.WithMessage("Cron expression is not valid.");

		RuleForEach(x => x.Recipients)
			.ChildRules(r =>
			{
				r.RuleFor(x => x.Email)
					.NotEmpty()
					.WithMessage("Recipient email is required.")
					.EmailAddress()
					.WithMessage("Recipient email is not valid.");
			});

		RuleForEach(x => x.Targets)
			.ChildRules(t =>
			{
				t.RuleFor(x => x.ScraperType)
					.Must(v => Enum.IsDefined(typeof(ScrapersEnum), v))
					.WithMessage("Invalid scraper type.");

				t.RuleFor(x => x.Url)
					.NotEmpty()
					.WithMessage("Target URL is required.");
			});
	}
}