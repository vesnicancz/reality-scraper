using Microsoft.Extensions.Logging;
using RealityScraper.Application.Abstractions.Database;
using RealityScraper.Application.Abstractions.Messaging;
using RealityScraper.Application.Interfaces.Repositories.Configuration;
using RealityScraper.Application.Interfaces.Scheduler;
using RealityScraper.SharedKernel;

namespace RealityScraper.Application.Features.ScraperTasks.RunNow;

internal sealed class RunTaskNowCommandHandler : ICommandHandler<RunTaskNowCommand>
{
	private readonly ITaskRepository taskRepository;
	private readonly IUnitOfWork unitOfWork;
	private readonly IDateTimeProvider dateTimeProvider;
	private readonly ISchedulerRefreshSignal schedulerRefreshSignal;
	private readonly ILogger<RunTaskNowCommandHandler> logger;

	public RunTaskNowCommandHandler(
		ITaskRepository taskRepository,
		IUnitOfWork unitOfWork,
		IDateTimeProvider dateTimeProvider,
		ISchedulerRefreshSignal schedulerRefreshSignal,
		ILogger<RunTaskNowCommandHandler> logger)
	{
		this.taskRepository = taskRepository;
		this.unitOfWork = unitOfWork;
		this.dateTimeProvider = dateTimeProvider;
		this.schedulerRefreshSignal = schedulerRefreshSignal;
		this.logger = logger;
	}

	public async Task<Result> Handle(RunTaskNowCommand command, CancellationToken cancellationToken)
	{
		var task = await taskRepository.GetByIdAsync(command.Id, cancellationToken);
		if (task == null)
		{
			return Result.Failure(Error.NotFound("Task.NotFound", $"Task with ID {command.Id} was not found."));
		}

		task.SetNextRunAt(dateTimeProvider.UtcNow);
		await unitOfWork.SaveChangesAsync(cancellationToken);

		schedulerRefreshSignal.RequestRefresh();

		logger.LogInformation("Task '{Name}' ({Id}) scheduled for immediate execution", task.Name, task.Id);

		return Result.Success();
	}
}
