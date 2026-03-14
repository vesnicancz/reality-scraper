using RealityScraper.Application.Abstractions.Messaging;

namespace RealityScraper.Application.Features.ScraperTasks.Delete;

public record DeleteScraperTaskCommand(Guid Id) : ICommand;