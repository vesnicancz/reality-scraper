using RealityScraper.Application.Abstractions.Messaging;
using RealityScraper.Application.Features.ScraperTaskRecipients;
using RealityScraper.Application.Features.ScraperTaskTargets;
using RealityScraper.Application.Interfaces.Repositories.Configuration;
using RealityScraper.SharedKernel;

namespace RealityScraper.Application.Features.ScraperTasks.GetById;

internal sealed class GetScraperTaskByIdQueryHandler : IQueryHandler<GetScraperTaskByIdQuery, ScraperTaskDto>
{
	private readonly IScraperTaskRepository scraperTaskRepository;

	public GetScraperTaskByIdQueryHandler(IScraperTaskRepository scraperTaskRepository)
	{
		this.scraperTaskRepository = scraperTaskRepository;
	}

	public async Task<Result<ScraperTaskDto>> Handle(GetScraperTaskByIdQuery query, CancellationToken cancellationToken)
	{
		var scraperTask = await scraperTaskRepository.GetTaskWithDetailsAsync(query.Id, cancellationToken);
		if (scraperTask == null)
		{
			return Result.Failure<ScraperTaskDto>(Error.NotFound("ScraperTask.NotFound", $"ScraperTask with ID {query.Id} was not found."));
		}

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