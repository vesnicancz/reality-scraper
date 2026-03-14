using RealityScraper.Application.Abstractions.Messaging;
using RealityScraper.Application.Features.ScraperTasks;
using RealityScraper.Application.Features.ScraperTasks.GetById;
using RealityScraper.Web.Api.Infrastructure;
using RealityScraper.Web.Api.Mappers.ScraperTasks;

namespace RealityScraper.Web.Api.Endpoints.ScraperTasks;

internal sealed class GetScraperTaskByIdEndpoint : IEndpoint
{
	public void MapEndpoint(IEndpointRouteBuilder app)
	{
		app.MapGet("/api/scraper-tasks/{id:guid}", async (
			Guid id,
			IQueryHandler<GetScraperTaskByIdQuery, ScraperTaskDto> queryHandler,
			CancellationToken cancellationToken) =>
		{
			var query = new GetScraperTaskByIdQuery(id);

			var result = await queryHandler.Handle(query, cancellationToken);

			return result.IsSuccess
				? Results.Ok(ScraperTaskDtoMapper.MapToResult(result.Value))
				: CustomResults.Problem(result);
		});
	}
}