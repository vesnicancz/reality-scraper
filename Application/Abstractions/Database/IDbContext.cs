using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using RealityScraper.SharedKernel;

namespace RealityScraper.Application.Abstractions.Database;

public interface IDbContext
{
	DatabaseFacade Database { get; }

	DbSet<T> Set<T>()
		where T : Entity;

	EntityEntry<TEntity> Entry<TEntity>(TEntity entity)
		where TEntity : Entity;

	Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}