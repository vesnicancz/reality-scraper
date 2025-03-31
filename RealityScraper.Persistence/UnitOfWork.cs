using RealityScraper.Application.Interfaces;
using RealityScraper.Persistence.Contexts;

namespace RealityScraper.Persistence;

public class UnitOfWork : IUnitOfWork
{
	private readonly IDbContext dbContext;

	public UnitOfWork(IDbContext dbContext)
	{
		this.dbContext = dbContext;
	}

	public async Task SaveChangesAsync(CancellationToken cancellationToken)
	{
		await dbContext.SaveChangesAsync(cancellationToken);
	}
}