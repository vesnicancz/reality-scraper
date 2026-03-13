using RealityScraper.Application.Abstractions.Messaging;
using RealityScraper.Application.Interfaces.Repositories.Configuration;
using RealityScraper.SharedKernel;

namespace RealityScraper.Application.Features.ScraperTasks.GetAll;

internal sealed class GetAllScraperTasksQueryHandler : IQueryHandler<GetAllScraperTasksQuery, List<ScraperTaskDto>>
{
	private readonly IScraperTaskRepository scraperTaskRepository;

	public GetAllScraperTasksQueryHandler(IScraperTaskRepository scraperTaskRepository)
	{
		this.scraperTaskRepository = scraperTaskRepository;
	}

	public async Task<Result<List<ScraperTaskDto>>> Handle(GetAllScraperTasksQuery query, CancellationToken cancellationToken)
	{
		var tasks = await scraperTaskRepository.GetAllAsync(cancellationToken);

		var result = tasks.Select(t => new ScraperTaskDto
		{
			Id = t.Id,
			Name = t.Name,
			CronExpression = t.CronExpression,
			Enabled = t.Enabled,
			LastRunAt = t.LastRunAt,
			NextRunAt = t.NextRunAt
		}).ToList();

		return Result.Success(result);
	}
}