using RealityScraper.Application.Abstractions.Database;
using RealityScraper.Application.Abstractions.Messaging;
using RealityScraper.Application.Interfaces.Repositories.Configuration;
using RealityScraper.Domain.Entities.Tasks;
using RealityScraper.SharedKernel;

namespace RealityScraper.Application.Features.ScraperTasks.Create;

internal sealed class CreateScraperTaskCommandHandler : ICommandHandler<CreateScraperTaskCommand, ScraperTaskDto>
{
	private readonly IScraperTaskRepository scraperTaskRepository;
	private readonly IDateTimeProvider dateTimeProvider;
	private readonly IUnitOfWork unitOfWork;

	public CreateScraperTaskCommandHandler(
		IScraperTaskRepository scraperTaskRepository,
		IDateTimeProvider dateTimeProvider,
		IUnitOfWork unitOfWork)
	{
		this.scraperTaskRepository = scraperTaskRepository;
		this.dateTimeProvider = dateTimeProvider;
		this.unitOfWork = unitOfWork;
	}

	public async Task<Result<ScraperTaskDto>> Handle(CreateScraperTaskCommand command, CancellationToken cancellationToken)
	{
		var scraperTask = new ScraperTask(command.Name, command.CronExpression, command.Enabled, dateTimeProvider.GetCurrentTime(), null);
		scraperTaskRepository.Add(scraperTask);

		await unitOfWork.SaveChangesAsync(cancellationToken);

		var result = new ScraperTaskDto
		{
			Id = scraperTask.Id,
			Name = scraperTask.Name,
			CronExpression = scraperTask.CronExpression,
			Enabled = scraperTask.Enabled
		};

		return Result.Success(result);
	}
}