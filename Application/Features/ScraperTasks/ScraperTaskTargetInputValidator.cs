using FluentValidation;
using RealityScraper.Domain.Enums;

namespace RealityScraper.Application.Features.ScraperTasks;

internal sealed class ScraperTaskTargetInputValidator : AbstractValidator<ScraperTaskTargetInput>
{
	public ScraperTaskTargetInputValidator()
	{
		RuleFor(x => x.ScraperType)
			.Must(v => Enum.IsDefined(typeof(ScrapersEnum), v))
			.WithMessage("Invalid scraper type.");

		RuleFor(x => x.Url)
			.NotEmpty()
			.WithMessage("Target URL is required.")
			.MaximumLength(500)
			.WithMessage("Target URL must not exceed 500 characters.")
			.Must(url => Uri.TryCreate(url, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
			.WithMessage("Target URL must be a valid HTTP or HTTPS URL.");
	}
}