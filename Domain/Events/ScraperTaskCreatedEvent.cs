using RealityScraper.SharedKernel;

namespace RealityScraper.Domain.Events;

public record ScraperTaskCreatedEvent(Guid ScraperTaskId) : IDomainEvent;