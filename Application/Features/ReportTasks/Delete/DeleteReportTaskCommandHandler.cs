using RealityScraper.Application.Abstractions.Database;
using RealityScraper.Application.Abstractions.Messaging;
using RealityScraper.Application.Interfaces.Repositories.Configuration;
using RealityScraper.Domain.Events;
using RealityScraper.SharedKernel;

namespace RealityScraper.Application.Features.ReportTasks.Delete;

internal sealed class DeleteReportTaskCommandHandler : ICommandHandler<DeleteReportTaskCommand>
{
	private readonly IReportTaskRepository reportTaskRepository;
	private readonly IUnitOfWork unitOfWork;

	public DeleteReportTaskCommandHandler(
		IReportTaskRepository reportTaskRepository,
		IUnitOfWork unitOfWork)
	{
		this.reportTaskRepository = reportTaskRepository;
		this.unitOfWork = unitOfWork;
	}

	public async Task<Result> Handle(DeleteReportTaskCommand command, CancellationToken cancellationToken)
	{
		var reportTask = await reportTaskRepository.GetByIdAsync(command.Id, cancellationToken);
		if (reportTask == null)
		{
			return Result.Failure(Error.NotFound("ReportTask.NotFound", $"ReportTask with ID {command.Id} was not found."));
		}

		reportTask.RaiseDomainEvents(new ReportTaskDeletedEvent(reportTask.Id, reportTask.Name));

		reportTaskRepository.Delete(reportTask);
		await unitOfWork.SaveChangesAsync(cancellationToken);

		return Result.Success();
	}
}
