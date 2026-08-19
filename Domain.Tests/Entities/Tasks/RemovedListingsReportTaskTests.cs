using RealityScraper.Domain.Entities.Tasks;

namespace RealityScraper.Domain.Tests.Entities.Tasks;

public class RemovedListingsReportTaskTests
{
	private static RemovedListingsReportTask CreateTask()
	{
		return new RemovedListingsReportTask(
			name: "Týdenní přehled stažených inzerátů",
			cronExpression: "0 7 * * 1",
			enabled: true,
			createdAt: new DateTimeOffset(2026, 1, 15, 7, 0, 0, TimeSpan.Zero),
			nextRunAt: new DateTimeOffset(2026, 1, 19, 7, 0, 0, TimeSpan.Zero));
	}

	[Fact]
	public void AddRecipient_SetsBackReferenceToTask()
	{
		var task = CreateTask();
		task.Id = Guid.NewGuid();
		var recipient = new ReportTaskRecipient("info@example.com");

		task.AddRecipient(recipient);

		Assert.Same(task, recipient.ReportTask);
		Assert.Equal(task.Id, recipient.ReportTaskId);
		Assert.Equal(recipient, Assert.Single(task.Recipients));
	}

	[Fact]
	public void RemoveRecipient_RemovesOnlyGivenRecipient()
	{
		var task = CreateTask();
		var first = new ReportTaskRecipient("prvni@example.com");
		var second = new ReportTaskRecipient("druhy@example.com");
		task.AddRecipient(first);
		task.AddRecipient(second);

		task.RemoveRecipient(first);

		Assert.Equal("druhy@example.com", Assert.Single(task.Recipients).Email);
	}

	[Fact]
	public void SetLastSuccessfulReportAt_IsNullUntilFirstReportIsSent()
	{
		var task = CreateTask();
		var sentAt = new DateTimeOffset(2026, 1, 19, 7, 0, 0, TimeSpan.Zero);

		Assert.Null(task.LastSuccessfulReportAt);

		task.SetLastSuccessfulReportAt(sentAt);

		Assert.Equal(sentAt, task.LastSuccessfulReportAt);
	}
}
