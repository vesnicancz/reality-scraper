namespace RealityScraper.Web.Shared.Models.ReportTasks;

public class ReportTaskResult
{
	public Guid Id { get; set; }

	public string Name { get; set; } = null!;

	public string CronExpression { get; set; } = null!;

	public bool Enabled { get; set; }

	public DateTimeOffset? LastRunAt { get; set; }

	public DateTimeOffset? NextRunAt { get; set; }

	public bool? LastRunSucceeded { get; set; }

	public string? LastRunLog { get; set; }

	public DateTimeOffset? LastSuccessfulReportAt { get; set; }

	public List<string> Recipients { get; set; } = [];

	public List<ReportTaskSourceResult> Sources { get; set; } = [];
}
