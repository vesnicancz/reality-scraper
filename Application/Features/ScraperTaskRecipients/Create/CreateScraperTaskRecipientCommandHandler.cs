using RealityScraper.Application.Abstractions.Database;
using RealityScraper.Application.Abstractions.Messaging;
using RealityScraper.Application.Interfaces.Repositories.Configuration;
using RealityScraper.Domain.Entities.Tasks;
using RealityScraper.SharedKernel;

namespace RealityScraper.Application.Features.ScraperTaskRecipients.Create;

internal sealed class CreateScraperTaskRecipientCommandHandler : ICommandHandler<CreateScraperTaskRecipientCommand, ScraperTaskRecipientDto>
{
	private readonly IScraperTaskRepository scraperTaskRepository;
	private readonly IUnitOfWork unitOfWork;

	public CreateScraperTaskRecipientCommandHandler(
		IScraperTaskRepository scraperTaskRepository,
		IUnitOfWork unitOfWork)
	{
		this.scraperTaskRepository = scraperTaskRepository;
		this.unitOfWork = unitOfWork;
	}

	public async Task<Result<ScraperTaskRecipientDto>> Handle(CreateScraperTaskRecipientCommand command, CancellationToken cancellationToken)
	{
		var scraperTask = await scraperTaskRepository.GetByIdAsync(command.ScraperTaskId, cancellationToken);
		if (scraperTask == null)
		{
			return Result.Failure<ScraperTaskRecipientDto>(Error.NotFound("ScraperTask.NotFound", $"ScraperTask with ID {command.ScraperTaskId} was not found."));
		}

		var scraperTaskRecipient = new ScraperTaskRecipient(command.Email);
		scraperTask.AddRecipient(scraperTaskRecipient);

		await unitOfWork.SaveChangesAsync(cancellationToken);

		var result = new ScraperTaskRecipientDto
		{
			Id = scraperTaskRecipient.Id,
			ScraperTaskId = scraperTaskRecipient.ScraperTaskId,
			Email = scraperTaskRecipient.Email
		};

		return Result.Success(result);
	}
}