using RealityScraper.Application.Abstractions.Database;
using RealityScraper.Application.Abstractions.Messaging;
using RealityScraper.Application.Interfaces.Repositories.Configuration;
using RealityScraper.Application.Interfaces.Scheduler;
using RealityScraper.Domain.Entities.Tasks;
using RealityScraper.Domain.Enums;
using RealityScraper.Domain.Events;
using RealityScraper.SharedKernel;

namespace RealityScraper.Application.Features.ScraperTasks.Update;

internal sealed class UpdateScraperTaskCommandHandler : ICommandHandler<UpdateScraperTaskCommand, ScraperTaskDto>
{
	private readonly IScraperTaskRepository scraperTaskRepository;
	private readonly IUnitOfWork unitOfWork;
	private readonly IScheduleTimeCalculator timeCalculator;
	private readonly IDateTimeProvider dateTimeProvider;

	public UpdateScraperTaskCommandHandler(
		IScraperTaskRepository scraperTaskRepository,
		IUnitOfWork unitOfWork,
		IScheduleTimeCalculator timeCalculator,
		IDateTimeProvider dateTimeProvider)
	{
		this.scraperTaskRepository = scraperTaskRepository;
		this.unitOfWork = unitOfWork;
		this.timeCalculator = timeCalculator;
		this.dateTimeProvider = dateTimeProvider;
	}

	public async Task<Result<ScraperTaskDto>> Handle(UpdateScraperTaskCommand command, CancellationToken cancellationToken)
	{
		var scraperTask = await scraperTaskRepository.GetTaskWithDetailsAsync(command.Id, cancellationToken);
		if (scraperTask == null)
		{
			return Result.Failure<ScraperTaskDto>(Error.NotFound("ScraperTask.NotFound", $"ScraperTask with ID {command.Id} was not found."));
		}

		var cronChanged = scraperTask.CronExpression != command.CronExpression;
		var enabledChanged = scraperTask.Enabled != command.Enabled;

		scraperTask.SetName(command.Name);
		scraperTask.SetCronExpression(command.CronExpression);
		scraperTask.SetEnabled(command.Enabled);

		if (cronChanged || enabledChanged)
		{
			var nextRunTime = command.Enabled
				? timeCalculator.GetNextExecutionTime(command.CronExpression, dateTimeProvider.UtcNow)
				: null;
			scraperTask.SetNextRunAt(nextRunTime);
		}

		// Replace recipients
		foreach (var existing in scraperTask.Recipients.ToList())
		{
			scraperTask.RemoveRecipient(existing);
		}

		foreach (var recipient in command.Recipients)
		{
			scraperTask.AddRecipient(new ScraperTaskRecipient(recipient.Email));
		}

		// Replace targets
		foreach (var existing in scraperTask.Targets.ToList())
		{
			scraperTask.RemoveTarget(existing);
		}

		foreach (var target in command.Targets)
		{
			scraperTask.AddTarget(new ScraperTaskTarget((ScrapersEnum)target.ScraperType, target.Url));
		}

		scraperTask.RaiseDomainEvents(new ScraperTaskUpdatedEvent(scraperTask.Id));

		scraperTaskRepository.Update(scraperTask);
		await unitOfWork.SaveChangesAsync(cancellationToken);

		return Result.Success(ScraperTaskMapper.MapToDetailDto(scraperTask));
	}
}