using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using RealityScraper.Application.Abstractions.Database;
using RealityScraper.SharedKernel;

namespace RealityScraper.Infrastructure.Repositories;

internal class Repository<T> : IRepository<T>
	where T : Entity
{
#pragma warning disable CA1051 // Do not declare visible instance fields
	protected readonly IDbContext dbContext;
	protected readonly DbSet<T> dbSet;
#pragma warning restore CA1051 // Do not declare visible instance fields

	public Repository(IDbContext dbContext)
	{
		this.dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
		dbSet = dbContext.Set<T>();
	}

	public void Add(T entity)
	{
		dbSet.Add(entity);
	}

	public void Delete(T entity)
	{
		dbSet.Remove(entity);
	}

	public async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
	{
		return await dbSet.FirstOrDefaultAsync(entity => entity.Id == id, cancellationToken);
	}

	public async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken)
	{
		return await dbSet.ToListAsync(cancellationToken);
	}

	public async Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken)
	{
		return await dbSet.AnyAsync(predicate, cancellationToken);
	}
}
