using RealityScraper.Application.Abstractions.Messaging;

namespace RealityScraper.Application.Features.ScraperTasks.Create;

public record CreateScraperTaskCommand(string Name, string CronExpression, bool Enabled) : ICommand<ScraperTaskDto>;