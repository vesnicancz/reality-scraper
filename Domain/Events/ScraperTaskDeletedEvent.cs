using RealityScraper.SharedKernel;

namespace RealityScraper.Domain.Events;

public record ScraperTaskDeletedEvent(Guid ScraperTaskId, string Name) : IDomainEvent;