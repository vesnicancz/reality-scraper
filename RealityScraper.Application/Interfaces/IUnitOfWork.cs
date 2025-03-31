namespace RealityScraper.Application.Interfaces;

public interface IUnitOfWork
{
	Task SaveChangesAsync(CancellationToken cancellationToken);
}