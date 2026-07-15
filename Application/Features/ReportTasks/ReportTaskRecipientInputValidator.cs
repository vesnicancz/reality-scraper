using FluentValidation;

namespace RealityScraper.Application.Features.ReportTasks;

internal sealed class ReportTaskRecipientInputValidator : AbstractValidator<ReportTaskRecipientInput>
{
	public ReportTaskRecipientInputValidator()
	{
		RuleFor(x => x.Email)
			.NotEmpty()
			.WithMessage("Recipient email is required.")
			.MaximumLength(100)
			.WithMessage("Recipient email must not exceed 100 characters.")
			.EmailAddress()
			.WithMessage("Recipient email is not valid.");
	}
}
