using RealityScraper.Application.Abstractions.Database;
using RealityScraper.Application.Abstractions.Messaging;
using RealityScraper.Application.Interfaces.Repositories.Configuration;
using RealityScraper.Application.Interfaces.Scheduler;
using RealityScraper.Domain.Entities.Tasks;
using RealityScraper.Domain.Events;
using RealityScraper.SharedKernel;

namespace RealityScraper.Application.Features.ReportTasks.Create;

internal sealed class CreateReportTaskCommandHandler : ICommandHandler<CreateReportTaskCommand, ReportTaskDto>
{
	private readonly IReportTaskRepository reportTaskRepository;
	private readonly IScraperTaskRepository scraperTaskRepository;
	private readonly IDateTimeProvider dateTimeProvider;
	private readonly IScheduleTimeCalculator timeCalculator;
	private readonly IUnitOfWork unitOfWork;

	public CreateReportTaskCommandHandler(
		IReportTaskRepository reportTaskRepository,
		IScraperTaskRepository scraperTaskRepository,
		IDateTimeProvider dateTimeProvider,
		IScheduleTimeCalculator timeCalculator,
		IUnitOfWork unitOfWork)
	{
		this.reportTaskRepository = reportTaskRepository;
		this.scraperTaskRepository = scraperTaskRepository;
		this.dateTimeProvider = dateTimeProvider;
		this.timeCalculator = timeCalculator;
		this.unitOfWork = unitOfWork;
	}

	public async Task<Result<ReportTaskDto>> Handle(CreateReportTaskCommand command, CancellationToken cancellationToken)
	{
		var missingScraperTask = await FindMissingScraperTaskAsync(command.ScraperTaskIds, cancellationToken);
		if (missingScraperTask != null)
		{
			return Result.Failure<ReportTaskDto>(Error.NotFound("ScraperTask.NotFound", $"ScraperTask with ID {missingScraperTask} was not found."));
		}

		var nextRunTime = command.Enabled
			? timeCalculator.GetNextExecutionTime(command.CronExpression, dateTimeProvider.UtcNow)
			: null;

		var reportTask = new RemovedListingsReportTask(command.Name, command.CronExpression, command.Enabled, dateTimeProvider.UtcNow, nextRunTime);

		foreach (var recipient in command.Recipients)
		{
			reportTask.AddRecipient(new ReportTaskRecipient(recipient.Email));
		}

		foreach (var scraperTaskId in command.ScraperTaskIds.Distinct())
		{
			reportTask.AddSource(new ReportTaskSource(scraperTaskId));
		}

		reportTask.RaiseDomainEvents(new ReportTaskCreatedEvent(reportTask.Id));

		reportTaskRepository.Add(reportTask);

		await unitOfWork.SaveChangesAsync(cancellationToken);

		return Result.Success(ReportTaskMapper.MapToDetailDto(reportTask));
	}

	private async Task<Guid?> FindMissingScraperTaskAsync(List<Guid> scraperTaskIds, CancellationToken cancellationToken)
	{
		foreach (var scraperTaskId in scraperTaskIds.Distinct())
		{
			if (!await scraperTaskRepository.AnyAsync(t => t.Id == scraperTaskId, cancellationToken))
			{
				return scraperTaskId;
			}
		}

		return null;
	}
}
