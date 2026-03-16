using Microsoft.Extensions.Logging;
using RealityScraper.Application.Abstractions.Database;
using RealityScraper.Application.Abstractions.Messaging;
using RealityScraper.Application.Interfaces.Repositories.Configuration;
using RealityScraper.Application.Interfaces.Scheduler;
using RealityScraper.SharedKernel;

namespace RealityScraper.Application.Features.ScraperTasks.RunNow;

internal sealed class RunScraperTaskNowCommandHandler : ICommandHandler<RunScraperTaskNowCommand>
{
	private readonly IScraperTaskRepository scraperTaskRepository;
	private readonly IUnitOfWork unitOfWork;
	private readonly IDateTimeProvider dateTimeProvider;
	private readonly ISchedulerRefreshSignal schedulerRefreshSignal;
	private readonly ILogger<RunScraperTaskNowCommandHandler> logger;

	public RunScraperTaskNowCommandHandler(
		IScraperTaskRepository scraperTaskRepository,
		IUnitOfWork unitOfWork,
		IDateTimeProvider dateTimeProvider,
		ISchedulerRefreshSignal schedulerRefreshSignal,
		ILogger<RunScraperTaskNowCommandHandler> logger)
	{
		this.scraperTaskRepository = scraperTaskRepository;
		this.unitOfWork = unitOfWork;
		this.dateTimeProvider = dateTimeProvider;
		this.schedulerRefreshSignal = schedulerRefreshSignal;
		this.logger = logger;
	}

	public async Task<Result> Handle(RunScraperTaskNowCommand command, CancellationToken cancellationToken)
	{
		var scraperTask = await scraperTaskRepository.GetTaskWithDetailsAsync(command.Id, cancellationToken);
		if (scraperTask == null)
		{
			return Result.Failure(Error.NotFound("ScraperTask.NotFound", $"ScraperTask with ID {command.Id} was not found."));
		}

		scraperTask.SetNextRunAt(dateTimeProvider.UtcNow);
		await unitOfWork.SaveChangesAsync(cancellationToken);

		schedulerRefreshSignal.RequestRefresh();

		logger.LogInformation("Task '{Name}' ({Id}) scheduled for immediate execution", scraperTask.Name, scraperTask.Id);

		return Result.Success();
	}
}
