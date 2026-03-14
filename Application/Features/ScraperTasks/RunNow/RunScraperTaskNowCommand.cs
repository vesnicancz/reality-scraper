using RealityScraper.Application.Abstractions.Messaging;

namespace RealityScraper.Application.Features.ScraperTasks.RunNow;

public record RunScraperTaskNowCommand(Guid Id) : ICommand;