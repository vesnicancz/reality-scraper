using RealityScraper.Application.Abstractions.Messaging;

namespace RealityScraper.Application.Features.ScraperTasks.GetAll;

public record GetAllScraperTasksQuery : IQuery<List<ScraperTaskDto>>;