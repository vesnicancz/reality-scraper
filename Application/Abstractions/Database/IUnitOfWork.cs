namespace RealityScraper.Application.Abstractions.Database;

public interface IUnitOfWork
{
	Task SaveChangesAsync(CancellationToken cancellationToken);
}