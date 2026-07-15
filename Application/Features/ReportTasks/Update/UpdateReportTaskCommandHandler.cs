using RealityScraper.Application.Abstractions.Database;
using RealityScraper.Application.Abstractions.Messaging;
using RealityScraper.Application.Interfaces.Repositories.Configuration;
using RealityScraper.Application.Interfaces.Scheduler;
using RealityScraper.Domain.Entities.Tasks;
using RealityScraper.Domain.Events;
using RealityScraper.SharedKernel;

namespace RealityScraper.Application.Features.ReportTasks.Update;

internal sealed class UpdateReportTaskCommandHandler : ICommandHandler<UpdateReportTaskCommand, ReportTaskDto>
{
	private readonly IReportTaskRepository reportTaskRepository;
	private readonly IScraperTaskRepository scraperTaskRepository;
	private readonly IUnitOfWork unitOfWork;
	private readonly IScheduleTimeCalculator timeCalculator;
	private readonly IDateTimeProvider dateTimeProvider;

	public UpdateReportTaskCommandHandler(
		IReportTaskRepository reportTaskRepository,
		IScraperTaskRepository scraperTaskRepository,
		IUnitOfWork unitOfWork,
		IScheduleTimeCalculator timeCalculator,
		IDateTimeProvider dateTimeProvider)
	{
		this.reportTaskRepository = reportTaskRepository;
		this.scraperTaskRepository = scraperTaskRepository;
		this.unitOfWork = unitOfWork;
		this.timeCalculator = timeCalculator;
		this.dateTimeProvider = dateTimeProvider;
	}

	public async Task<Result<ReportTaskDto>> Handle(UpdateReportTaskCommand command, CancellationToken cancellationToken)
	{
		var reportTask = await reportTaskRepository.GetTaskWithDetailsAsync(command.Id, cancellationToken);
		if (reportTask == null)
		{
			return Result.Failure<ReportTaskDto>(Error.NotFound("ReportTask.NotFound", $"ReportTask with ID {command.Id} was not found."));
		}

		foreach (var scraperTaskId in command.ScraperTaskIds.Distinct())
		{
			if (!await scraperTaskRepository.AnyAsync(t => t.Id == scraperTaskId, cancellationToken))
			{
				return Result.Failure<ReportTaskDto>(Error.NotFound("ScraperTask.NotFound", $"ScraperTask with ID {scraperTaskId} was not found."));
			}
		}

		var cronChanged = reportTask.CronExpression != command.CronExpression;
		var enabledChanged = reportTask.Enabled != command.Enabled;

		reportTask.SetName(command.Name);
		reportTask.SetCronExpression(command.CronExpression);
		reportTask.SetEnabled(command.Enabled);

		if (cronChanged || enabledChanged)
		{
			var nextRunTime = command.Enabled
				? timeCalculator.GetNextExecutionTime(command.CronExpression, dateTimeProvider.UtcNow)
				: null;
			reportTask.SetNextRunAt(nextRunTime);
		}

		// Replace recipients
		foreach (var existing in reportTask.Recipients.ToList())
		{
			reportTask.RemoveRecipient(existing);
		}

		foreach (var recipient in command.Recipients)
		{
			reportTask.AddRecipient(new ReportTaskRecipient(recipient.Email));
		}

		// Replace sources
		foreach (var existing in reportTask.Sources.ToList())
		{
			reportTask.RemoveSource(existing);
		}

		foreach (var scraperTaskId in command.ScraperTaskIds.Distinct())
		{
			reportTask.AddSource(new ReportTaskSource(scraperTaskId));
		}

		reportTask.RaiseDomainEvents(new ReportTaskUpdatedEvent(reportTask.Id));

		reportTaskRepository.Update(reportTask);
		await unitOfWork.SaveChangesAsync(cancellationToken);

		return Result.Success(ReportTaskMapper.MapToDetailDto(reportTask));
	}
}
