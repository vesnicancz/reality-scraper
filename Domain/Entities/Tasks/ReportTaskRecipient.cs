using RealityScraper.SharedKernel;

namespace RealityScraper.Domain.Entities.Tasks;

public class ReportTaskRecipient : Entity
{
	public Guid ReportTaskId { get; protected set; }

	public RemovedListingsReportTask ReportTask { get; protected set; } = null!;

	public string Email { get; protected set; } = null!;

	protected ReportTaskRecipient()
	{
	}

	public ReportTaskRecipient(string email)
	{
		Email = email;
	}

	public void SetReportTask(RemovedListingsReportTask reportTask)
	{
		ReportTaskId = reportTask.Id;
		ReportTask = reportTask;
	}
}
