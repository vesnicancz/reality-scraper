using RealityScraper.SharedKernel;

namespace RealityScraper.Domain.Events;

public record ReportTaskDeletedEvent(Guid ReportTaskId, string Name) : IDomainEvent;
