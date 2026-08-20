using RealityScraper.Application.Abstractions.Messaging;

namespace RealityScraper.Application.Features.Dashboard.GetSummary;

public record GetDashboardSummaryQuery : IQuery<DashboardSummaryDto>;