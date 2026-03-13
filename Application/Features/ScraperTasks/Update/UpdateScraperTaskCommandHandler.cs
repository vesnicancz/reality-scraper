using RealityScraper.Application.Abstractions.Database;
using RealityScraper.Application.Abstractions.Messaging;
using RealityScraper.Application.Features.ScraperTaskRecipients;
using RealityScraper.Application.Features.ScraperTaskTargets;
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

		scraperTask.SetName(command.Name);
		scraperTask.SetCronExpression(command.CronExpression);
		scraperTask.SetEnabled(command.Enabled);

		var nextRunTime = command.Enabled
			? timeCalculator.GetNextExecutionTime(command.CronExpression, dateTimeProvider.GetCurrentTime())
			: null;
		scraperTask.SetNextRunAt(nextRunTime);

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

		var result = new ScraperTaskDto
		{
			Id = scraperTask.Id,
			Name = scraperTask.Name,
			CronExpression = scraperTask.CronExpression,
			Enabled = scraperTask.Enabled,
			Recipients = scraperTask.Recipients.Select(r => new ScraperTaskRecipientDto
			{
				Id = r.Id,
				ScraperTaskId = r.ScraperTaskId,
				Email = r.Email
			}).ToList(),
			Targets = scraperTask.Targets.Select(t => new ScraperTaskTargetDto
			{
				Id = t.Id,
				ScraperTaskId = t.ScraperTaskId,
				ScraperType = (int)t.ScraperType,
				Url = t.Url
			}).ToList()
		};

		return Result.Success(result);
	}
}