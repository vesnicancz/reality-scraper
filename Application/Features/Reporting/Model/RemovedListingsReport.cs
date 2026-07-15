namespace RealityScraper.Application.Features.Reporting.Model;

public record RemovedListingsReport
{
	public string ReportName { get; init; } = string.Empty;

	public DateTimeOffset ReportDate { get; init; }

	public DateTimeOffset PeriodFrom { get; init; }

	public DateTimeOffset PeriodTo { get; init; }

	public List<RemovedListingsTaskSection> Sections { get; init; } = new List<RemovedListingsTaskSection>();

	public int RemovedListingsCount => Sections.Sum(s => s.Listings.Count);
}
