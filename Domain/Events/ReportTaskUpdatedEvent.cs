using RealityScraper.SharedKernel;

namespace RealityScraper.Domain.Events;

public record ReportTaskUpdatedEvent(Guid ReportTaskId) : IDomainEvent;
