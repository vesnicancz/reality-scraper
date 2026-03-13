using RealityScraper.Application.Abstractions.Messaging;
using RealityScraper.Application.Features.ScraperTasks;
using RealityScraper.Application.Features.ScraperTasks.Create;
using RealityScraper.Application.Features.ScraperTasks.Update;
using RealityScraper.Web.Api.Infrastructure;
using RealityScraper.Web.Api.Mappers.ScraperTasks;
using RealityScraper.Web.Shared.Models.ScraperTasks;

namespace RealityScraper.Web.Api.Endpoints.ScraperTasks;

internal sealed class UpdateScraperTaskEndpoint : IEndpoint
{
	public void MapEndpoint(IEndpointRouteBuilder app)
	{
		app.MapPut("/api/scraper-tasks/{id:guid}", async (
			Guid id,
			UpdateScraperTaskRequest request,
			ICommandHandler<UpdateScraperTaskCommand, ScraperTaskDto> commandHandler,
			CancellationToken cancellationToken) =>
		{
			var command = new UpdateScraperTaskCommand(
				id,
				request.Name,
				request.CronExpression,
				request.Enabled,
				request.Recipients.Select(r => new CreateScraperTaskRecipientInput(r.Email)).ToList(),
				request.Targets.Select(t => new CreateScraperTaskTargetInput(t.ScraperType, t.Url)).ToList());

			var result = await commandHandler.Handle(command, cancellationToken);

			return result.IsSuccess
				? Results.Ok(ScraperTaskDtoMapper.MapToResult(result.Value))
				: CustomResults.Problem(result);
		});
	}
}