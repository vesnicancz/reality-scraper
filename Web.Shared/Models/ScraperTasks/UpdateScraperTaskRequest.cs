using FluentValidation;

namespace RealityScraper.Web.Shared.Models.ScraperTasks;

public class UpdateScraperTaskRequest : ScraperTaskInputModel
{
	public class UpdateScraperTaskRequestValidator : AbstractValidator<UpdateScraperTaskRequest>
	{
		public UpdateScraperTaskRequestValidator()
		{
			RuleFor(x => x.Name)
				.NotEmpty()
				.WithMessage("Name is required.");
		}
	}
}