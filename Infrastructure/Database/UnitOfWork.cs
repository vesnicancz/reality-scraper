using RealityScraper.Application.Abstractions.Database;

namespace RealityScraper.Infrastructure.Database;

internal class UnitOfWork : IUnitOfWork
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