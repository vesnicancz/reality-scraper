using RealityScraper.Application.Abstractions.Database;
using RealityScraper.Application.Abstractions.Messaging;
using RealityScraper.Application.Features.ScraperTaskRecipients;
using RealityScraper.Application.Features.ScraperTaskTargets;
using RealityScraper.Application.Interfaces.Repositories.Configuration;
using RealityScraper.Domain.Entities.Tasks;
using RealityScraper.Domain.Enums;
using RealityScraper.Domain.Events;
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

		var result = new ScraperTaskDto
		{
			Id = scraperTask.Id,
			Name = scraperTask.Name,
			CronExpression = scraperTask.CronExpression,
			Enabled = scraperTask.Enabled,
			LastRunAt = scraperTask.LastRunAt,
			NextRunAt = scraperTask.NextRunAt,
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