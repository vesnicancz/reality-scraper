using RealityScraper.Application.Abstractions.Messaging;
using RealityScraper.Application.Features.ScraperTasks;
using RealityScraper.Application.Features.ScraperTasks.GetAll;
using RealityScraper.Web.Api.Infrastructure;
using RealityScraper.Web.Api.Mappers.ScraperTasks;

namespace RealityScraper.Web.Api.Endpoints.ScraperTasks;

internal sealed class GetAllScraperTasksEndpoint : IEndpoint
{
	public void MapEndpoint(IEndpointRouteBuilder app)
	{
		app.MapGet("/api/scraper-tasks", async (
			IQueryHandler<GetAllScraperTasksQuery, List<ScraperTaskDto>> queryHandler,
			CancellationToken cancellationToken) =>
		{
			var query = new GetAllScraperTasksQuery();

			var result = await queryHandler.Handle(query, cancellationToken);

			return result.IsSuccess
				? Results.Ok(result.Value.Select(ScraperTaskDtoMapper.MapToResult).ToList())
				: CustomResults.Problem(result);
		});
	}
}