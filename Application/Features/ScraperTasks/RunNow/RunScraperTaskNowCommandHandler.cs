using RealityScraper.Application.Abstractions.Messaging;
using RealityScraper.Application.Features.Scraping;
using RealityScraper.Application.Interfaces.Repositories.Configuration;
using RealityScraper.Application.Interfaces.Scheduler;
using RealityScraper.SharedKernel;

namespace RealityScraper.Application.Features.ScraperTasks.RunNow;

internal sealed class RunScraperTaskNowCommandHandler : ICommandHandler<RunScraperTaskNowCommand>
{
	private readonly IScraperTaskRepository scraperTaskRepository;
	private readonly ScraperServiceTask scraperServiceTask;
	private readonly ITaskSchedulerService taskSchedulerService;
	private readonly IScheduleTimeCalculator timeCalculator;
	private readonly IDateTimeProvider dateTimeProvider;

	public RunScraperTaskNowCommandHandler(
		IScraperTaskRepository scraperTaskRepository,
		ScraperServiceTask scraperServiceTask,
		ITaskSchedulerService taskSchedulerService,
		IScheduleTimeCalculator timeCalculator,
		IDateTimeProvider dateTimeProvider)
	{
		this.scraperTaskRepository = scraperTaskRepository;
		this.scraperServiceTask = scraperServiceTask;
		this.taskSchedulerService = taskSchedulerService;
		this.timeCalculator = timeCalculator;
		this.dateTimeProvider = dateTimeProvider;
	}

	public async Task<Result> Handle(RunScraperTaskNowCommand command, CancellationToken cancellationToken)
	{
		var scraperTask = await scraperTaskRepository.GetTaskWithDetailsAsync(command.Id, cancellationToken);
		if (scraperTask == null)
		{
			return Result.Failure(Error.NotFound("ScraperTask.NotFound", $"ScraperTask with ID {command.Id} was not found."));
		}

		var config = taskSchedulerService.CreateScrapingConfigFromTask(scraperTask);
		var succeeded = true;

		try
		{
			await scraperServiceTask.ExecuteAsync(config, cancellationToken);
		}
		catch (Exception) when (!cancellationToken.IsCancellationRequested)
		{
			succeeded = false;
		}

		var lastRunTime = dateTimeProvider.GetCurrentTime();
		var nextRunTime = scraperTask.Enabled
			? timeCalculator.GetNextExecutionTime(scraperTask.CronExpression, lastRunTime)
			: null;

		await taskSchedulerService.UpdateTaskExecutionTimesAsync(scraperTask.Id, lastRunTime, nextRunTime, cancellationToken);

		return succeeded
			? Result.Success()
			: Result.Failure(Error.Failure("ScraperTask.RunFailed", $"Task '{scraperTask.Name}' failed during execution."));
	}
}