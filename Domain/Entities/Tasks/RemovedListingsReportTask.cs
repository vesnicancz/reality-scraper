namespace RealityScraper.Domain.Entities.Tasks;

public class RemovedListingsReportTask : TaskBase
{
	private List<ReportTaskRecipient> recipients = new List<ReportTaskRecipient>();
	public IReadOnlyList<ReportTaskRecipient> Recipients => recipients;

	private List<ReportTaskSource> sources = new List<ReportTaskSource>();
	public IReadOnlyList<ReportTaskSource> Sources => sources;

	public DateTimeOffset? LastSuccessfulReportAt { get; protected set; }

	protected RemovedListingsReportTask()
	{
	}

	public RemovedListingsReportTask(string name, string cronExpression, bool enabled, DateTimeOffset createdAt, DateTimeOffset? nextRunAt)
	{
		Name = name;
		CronExpression = cronExpression;
		Enabled = enabled;
		CreatedAt = createdAt;
		NextRunAt = nextRunAt;
	}

	public void SetLastSuccessfulReportAt(DateTimeOffset lastSuccessfulReportAt)
	{
		LastSuccessfulReportAt = lastSuccessfulReportAt;
	}

	public void AddRecipient(ReportTaskRecipient recipient)
	{
		recipient.SetReportTask(this);
		recipients.Add(recipient);
	}

	public void RemoveRecipient(ReportTaskRecipient recipient)
	{
		recipients.Remove(recipient);
	}

	public void AddSource(ReportTaskSource source)
	{
		source.SetReportTask(this);
		sources.Add(source);
	}

	public void RemoveSource(ReportTaskSource source)
	{
		sources.Remove(source);
	}
}
