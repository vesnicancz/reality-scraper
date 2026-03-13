using RealityScraper.Application.Abstractions.Database;
using RealityScraper.Application.Abstractions.Events;

namespace RealityScraper.Infrastructure.Database;

internal class UnitOfWork : IUnitOfWork
{
	private readonly IDbContext dbContext;
	private readonly IDomainEventDispatcher domainEventDispatcher;

	public UnitOfWork(IDbContext dbContext, IDomainEventDispatcher domainEventDispatcher)
	{
		this.dbContext = dbContext;
		this.domainEventDispatcher = domainEventDispatcher;
	}

	public async Task SaveChangesAsync(CancellationToken cancellationToken)
	{
		await dbContext.SaveChangesAsync(cancellationToken);
		await domainEventDispatcher.DispatchEventsAsync(cancellationToken);
	}
}