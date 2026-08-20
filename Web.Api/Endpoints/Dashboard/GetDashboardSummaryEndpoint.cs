using RealityScraper.Application.Abstractions.Messaging;
using RealityScraper.Application.Features.Dashboard;
using RealityScraper.Application.Features.Dashboard.GetSummary;
using RealityScraper.Web.Api.Infrastructure;
using RealityScraper.Web.Api.Mappers.Dashboard;

namespace RealityScraper.Web.Api.Endpoints.Dashboard;

internal sealed class GetDashboardSummaryEndpoint : IEndpoint
{
	public void MapEndpoint(IEndpointRouteBuilder app)
	{
		app.MapGet("/api/dashboard", async (
			IQueryHandler<GetDashboardSummaryQuery, DashboardSummaryDto> queryHandler,
			CancellationToken cancellationToken) =>
		{
			var result = await queryHandler.Handle(new GetDashboardSummaryQuery(), cancellationToken);

			return result.IsSuccess
				? Results.Ok(DashboardDtoMapper.MapToResult(result.Value))
				: CustomResults.Problem(result);
		});
	}
}