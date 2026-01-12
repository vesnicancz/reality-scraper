using RealityScraper.Application.Abstractions.Messaging;

namespace RealityScraper.Application.Features.ScraperTaskTargets.Create;

public record CreateScraperTaskTargetCommand(Guid ScraperTaskId, int ScraperType, string Url) : ICommand<ScraperTaskTargetDto>;