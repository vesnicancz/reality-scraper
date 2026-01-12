using FluentValidation;

namespace RealityScraper.Web.Shared.Models.ScraperTasks;

public class CreateScraperTaskRequest : ScraperTaskInputModel
{
	public class CreateScraperTaskRequestValidator : AbstractValidator<CreateScraperTaskRequest>
	{
		public CreateScraperTaskRequestValidator()
		{
			RuleFor(x => x.Name)
				.NotEmpty()
				.WithMessage("Name is required.");
		}
	}
}