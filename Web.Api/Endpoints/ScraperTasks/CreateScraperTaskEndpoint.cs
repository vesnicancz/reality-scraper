using RealityScraper.Application.Abstractions.Messaging;
using RealityScraper.Application.Features.ScraperTasks;
using RealityScraper.Application.Features.ScraperTasks.Create;
using RealityScraper.Web.Api.Infrastructure;
using RealityScraper.Web.Api.Mappers.ScraperTasks;
using RealityScraper.Web.Shared.Models.ScraperTasks;

namespace RealityScraper.Web.Api.Endpoints.ScraperTasks;

internal sealed class CreateScraperTaskEndpoint : IEndpoint
{
	public void MapEndpoint(IEndpointRouteBuilder app)
	{
		app.MapPost("/api/scraper-tasks", async (
			CreateScraperTaskRequest request,
			ICommandHandler<CreateScraperTaskCommand, ScraperTaskDto> commandHandler,
			CancellationToken cancellationToken) =>
		{
			var command = new CreateScraperTaskCommand(
				request.Name,
				request.CronExpression,
				request.Enabled,
				(request.Recipients ?? []).Select(r => new ScraperTaskRecipientInput(r.Email)).ToList(),
				(request.Targets ?? []).Select(t => new ScraperTaskTargetInput(t.ScraperType, t.Url)).ToList());

			var result = await commandHandler.Handle(command, cancellationToken);

			return result.IsSuccess
				? Results.Created($"/api/scraper-tasks/{result.Value.Id}", ScraperTaskDtoMapper.MapToResult(result.Value))
				: CustomResults.Problem(result);
		});
	}
}