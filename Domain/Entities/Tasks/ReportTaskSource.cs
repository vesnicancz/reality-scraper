using RealityScraper.SharedKernel;

namespace RealityScraper.Domain.Entities.Tasks;

public class ReportTaskSource : Entity
{
	public Guid ReportTaskId { get; protected set; }

	public RemovedListingsReportTask ReportTask { get; protected set; } = null!;

	public Guid ScraperTaskId { get; protected set; }

	public ScraperTask ScraperTask { get; protected set; } = null!;

	protected ReportTaskSource()
	{
	}

	public ReportTaskSource(Guid scraperTaskId)
	{
		ScraperTaskId = scraperTaskId;
	}

	public ReportTaskSource(ScraperTask scraperTask)
	{
		ScraperTaskId = scraperTask.Id;
		ScraperTask = scraperTask;
	}

	public void SetReportTask(RemovedListingsReportTask reportTask)
	{
		ReportTaskId = reportTask.Id;
		ReportTask = reportTask;
	}
}
