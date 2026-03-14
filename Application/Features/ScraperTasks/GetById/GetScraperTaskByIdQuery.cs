using RealityScraper.Application.Abstractions.Messaging;

namespace RealityScraper.Application.Features.ScraperTasks.GetById;

public record GetScraperTaskByIdQuery(Guid Id) : IQuery<ScraperTaskDto>;