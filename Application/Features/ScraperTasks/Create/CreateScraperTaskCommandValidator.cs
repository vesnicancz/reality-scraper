using FluentValidation;
using RealityScraper.Application.Interfaces.Scheduler;

namespace RealityScraper.Application.Features.ScraperTasks.Create;

internal sealed class CreateScraperTaskCommandValidator : AbstractValidator<CreateScraperTaskCommand>
{
	public CreateScraperTaskCommandValidator(IScheduleTimeCalculator timeCalculator)
	{
		Include(new ScraperTaskCommandValidator(timeCalculator));
	}
}
