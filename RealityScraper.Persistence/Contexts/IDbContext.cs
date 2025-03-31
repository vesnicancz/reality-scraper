using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using RealityScraper.Domain.Common;

namespace RealityScraper.Persistence.Contexts;

public interface IDbContext
{
	DbSet<T> Set<T>()
		where T : BaseEntity;

	EntityEntry<TEntity> Entry<TEntity>(TEntity entity)
		where TEntity : BaseEntity;

	Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}