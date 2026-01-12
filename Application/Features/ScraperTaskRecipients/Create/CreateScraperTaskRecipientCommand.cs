using RealityScraper.Application.Abstractions.Messaging;

namespace RealityScraper.Application.Features.ScraperTaskRecipients.Create;

public record CreateScraperTaskRecipientCommand(Guid ScraperTaskId, string Email) : ICommand<ScraperTaskRecipientDto>;