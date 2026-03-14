using FluentValidation;
using RealityScraper.Application.Interfaces.Scheduler;

namespace RealityScraper.Application.Features.ScraperTasks.Update;

internal sealed class UpdateScraperTaskCommandValidator : AbstractValidator<UpdateScraperTaskCommand>
{
	public UpdateScraperTaskCommandValidator(IScheduleTimeCalculator timeCalculator)
	{
		RuleFor(x => x.Id)
			.NotEmpty()
			.WithMessage("ScraperTask ID is required.");

		Include(new ScraperTaskCommandValidator(timeCalculator));
	}
}
