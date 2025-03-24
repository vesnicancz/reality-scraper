namespace RealityScraper.Data;

public class UnitOfWork : IUnitOfWork
{
	private readonly RealityDbContext realityDbContext;

	public UnitOfWork(RealityDbContext realityDbContext)
	{
		this.realityDbContext = realityDbContext;
	}

	public async Task SaveChangesAsync(CancellationToken cancellationToken)
	{
		await realityDbContext.SaveChangesAsync(cancellationToken);
	}
}