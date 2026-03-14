using RealityScraper.Application.Abstractions.Messaging;
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

		return Result.Success(ScraperTaskMapper.MapToDetailDto(scraperTask));
	}
}