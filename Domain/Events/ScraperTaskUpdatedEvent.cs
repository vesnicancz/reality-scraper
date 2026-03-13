using RealityScraper.SharedKernel;

namespace RealityScraper.Domain.Events;

public record ScraperTaskUpdatedEvent(Guid ScraperTaskId) : IDomainEvent;