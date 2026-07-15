using RealityScraper.SharedKernel;

namespace RealityScraper.Domain.Events;

public record ReportTaskCreatedEvent(Guid ReportTaskId) : IDomainEvent;
