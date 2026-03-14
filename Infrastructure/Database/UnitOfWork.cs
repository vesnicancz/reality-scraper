using Microsoft.Extensions.Logging;
using RealityScraper.Application.Abstractions.Database;
using RealityScraper.Application.Abstractions.Events;

namespace RealityScraper.Infrastructure.Database;

internal class UnitOfWork : IUnitOfWork
{
	private readonly IDbContext dbContext;
	private readonly IDomainEventDispatcher domainEventDispatcher;
	private readonly ILogger<UnitOfWork> logger;

	public UnitOfWork(IDbContext dbContext, IDomainEventDispatcher domainEventDispatcher, ILogger<UnitOfWork> logger)
	{
		this.dbContext = dbContext;
		this.domainEventDispatcher = domainEventDispatcher;
		this.logger = logger;
	}

	public async Task SaveChangesAsync(CancellationToken cancellationToken)
	{
		domainEventDispatcher.CollectEvents();

		await dbContext.SaveChangesAsync(cancellationToken);

		try
		{
			await domainEventDispatcher.DispatchEventsAsync(cancellationToken);
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Failed to dispatch domain events after saving changes");
		}
	}
}