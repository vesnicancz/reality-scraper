using RealityScraper.Application.Abstractions.Database;
using RealityScraper.Application.Abstractions.Messaging;
using RealityScraper.Application.Interfaces.Repositories.Configuration;
using RealityScraper.Application.Interfaces.Scheduler;
using RealityScraper.Domain.Entities.Tasks;
using RealityScraper.Domain.Enums;
using RealityScraper.Domain.Events;
using RealityScraper.SharedKernel;

namespace RealityScraper.Application.Features.ScraperTasks.Create;

internal sealed class CreateScraperTaskCommandHandler : ICommandHandler<CreateScraperTaskCommand, ScraperTaskDto>
{
	private readonly IScraperTaskRepository scraperTaskRepository;
	private readonly IDateTimeProvider dateTimeProvider;
	private readonly IScheduleTimeCalculator timeCalculator;
	private readonly IUnitOfWork unitOfWork;

	public CreateScraperTaskCommandHandler(
		IScraperTaskRepository scraperTaskRepository,
		IDateTimeProvider dateTimeProvider,
		IScheduleTimeCalculator timeCalculator,
		IUnitOfWork unitOfWork)
	{
		this.scraperTaskRepository = scraperTaskRepository;
		this.dateTimeProvider = dateTimeProvider;
		this.timeCalculator = timeCalculator;
		this.unitOfWork = unitOfWork;
	}

	public async Task<Result<ScraperTaskDto>> Handle(CreateScraperTaskCommand command, CancellationToken cancellationToken)
	{
		var nextRunTime = command.Enabled
			? timeCalculator.GetNextExecutionTime(command.CronExpression, dateTimeProvider.GetCurrentTime())
			: null;

		var scraperTask = new ScraperTask(command.Name, command.CronExpression, command.Enabled, dateTimeProvider.GetCurrentTime(), nextRunTime);

		foreach (var recipient in command.Recipients)
		{
			scraperTask.AddRecipient(new ScraperTaskRecipient(recipient.Email));
		}

		foreach (var target in command.Targets)
		{
			scraperTask.AddTarget(new ScraperTaskTarget((ScrapersEnum)target.ScraperType, target.Url));
		}

		scraperTask.RaiseDomainEvents(new ScraperTaskCreatedEvent(scraperTask.Id));

		scraperTaskRepository.Add(scraperTask);

		await unitOfWork.SaveChangesAsync(cancellationToken);

		return Result.Success(ScraperTaskMapper.MapToDetailDto(scraperTask));
	}
}