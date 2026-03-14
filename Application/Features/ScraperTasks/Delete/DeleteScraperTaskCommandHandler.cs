using RealityScraper.Application.Abstractions.Database;
using RealityScraper.Application.Abstractions.Messaging;
using RealityScraper.Application.Interfaces.Repositories.Configuration;
using RealityScraper.Domain.Events;
using RealityScraper.SharedKernel;

namespace RealityScraper.Application.Features.ScraperTasks.Delete;

internal sealed class DeleteScraperTaskCommandHandler : ICommandHandler<DeleteScraperTaskCommand>
{
	private readonly IScraperTaskRepository scraperTaskRepository;
	private readonly IUnitOfWork unitOfWork;

	public DeleteScraperTaskCommandHandler(
		IScraperTaskRepository scraperTaskRepository,
		IUnitOfWork unitOfWork)
	{
		this.scraperTaskRepository = scraperTaskRepository;
		this.unitOfWork = unitOfWork;
	}

	public async Task<Result> Handle(DeleteScraperTaskCommand command, CancellationToken cancellationToken)
	{
		var scraperTask = await scraperTaskRepository.GetByIdAsync(command.Id, cancellationToken);
		if (scraperTask == null)
		{
			return Result.Failure(Error.NotFound("ScraperTask.NotFound", $"ScraperTask with ID {command.Id} was not found."));
		}

		scraperTask.RaiseDomainEvents(new ScraperTaskDeletedEvent(scraperTask.Id, scraperTask.Name));

		scraperTaskRepository.Delete(scraperTask);
		await unitOfWork.SaveChangesAsync(cancellationToken);

		return Result.Success();
	}
}