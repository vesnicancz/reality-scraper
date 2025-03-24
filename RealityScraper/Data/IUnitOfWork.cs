namespace RealityScraper.Data;

public interface IUnitOfWork
{
	Task SaveChangesAsync(CancellationToken cancellationToken);
}