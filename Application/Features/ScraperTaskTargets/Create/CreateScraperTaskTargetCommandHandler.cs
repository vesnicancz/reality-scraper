using RealityScraper.Application.Abstractions.Database;
using RealityScraper.Application.Abstractions.Messaging;
using RealityScraper.Application.Interfaces.Repositories.Configuration;
using RealityScraper.Domain.Entities.Tasks;
using RealityScraper.Domain.Enums;
using RealityScraper.SharedKernel;

namespace RealityScraper.Application.Features.ScraperTaskTargets.Create;

internal sealed class CreateScraperTaskTargetCommandHandler : ICommandHandler<CreateScraperTaskTargetCommand, ScraperTaskTargetDto>
{
	private readonly IScraperTaskRepository scraperTaskRepository;
	private readonly IUnitOfWork unitOfWork;

	public CreateScraperTaskTargetCommandHandler(
		IScraperTaskRepository scraperTaskRepository,
		IUnitOfWork unitOfWork)
	{
		this.scraperTaskRepository = scraperTaskRepository;
		this.unitOfWork = unitOfWork;
	}

	public async Task<Result<ScraperTaskTargetDto>> Handle(CreateScraperTaskTargetCommand command, CancellationToken cancellationToken)
	{
		var scraperTask = await scraperTaskRepository.GetByIdAsync(command.ScraperTaskId, cancellationToken);
		if (scraperTask == null)
		{
			return Result.Failure<ScraperTaskTargetDto>(Error.NotFound("ScraperTask.NotFound", $"ScraperTask with ID {command.ScraperTaskId} was not found."));
		}

		var scraperType = (ScrapersEnum)command.ScraperType;
		var scraperTaskTarget = new ScraperTaskTarget(scraperType, command.Url);

		scraperTask.AddTarget(scraperTaskTarget);

		await unitOfWork.SaveChangesAsync(cancellationToken);

		var result = new ScraperTaskTargetDto()
		{
			Id = scraperTaskTarget.Id,
			ScraperTaskId = scraperTaskTarget.ScraperTaskId,
			ScraperType = (int)scraperTaskTarget.ScraperType,
			Url = scraperTaskTarget.Url
		};

		return Result.Success(result);
	}
}