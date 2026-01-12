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

	//public async Task<T> AddAsync(T entity, CancellationToken cancellationToken)
	//{
	//	await dbSet.AddAsync(entity, cancellationToken);
	//	return entity;
	//}

	public void Update(T entity)
	{
		dbSet.Attach(entity);
		dbContext.Entry(entity).State = EntityState.Modified;
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

	public async Task<IReadOnlyList<T>> GetAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken)
	{
		return await dbSet.Where(predicate).ToListAsync(cancellationToken);
	}

	public async Task<IReadOnlyList<T>> GetAsync(
		Expression<Func<T, bool>>? predicate = null,
		Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
		List<Expression<Func<T, object>>>? includes = null,
		bool disableTracking = true,
		CancellationToken cancellationToken = default) // TODO: PM cancellation token s default?
	{
		IQueryable<T> query = dbSet;

		if (disableTracking)
		{
			query = query.AsNoTracking();
		}

		if (includes != null)
		{
			query = includes.Aggregate(query, (current, include) => current.Include(include));
		}

		if (predicate != null)
		{
			query = query.Where(predicate);
		}

		if (orderBy != null)
		{
			return await orderBy(query).ToListAsync(cancellationToken);
		}

		return await query.ToListAsync(cancellationToken);
	}

	public async Task<T?> GetFirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken)
	{
		return await GetFirstOrDefaultAsync(predicate, null, cancellationToken);
	}

	public async Task<T?> GetFirstOrDefaultAsync(
		Expression<Func<T, bool>>? predicate,
		Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy,
		CancellationToken cancellationToken)
	{
		IQueryable<T> query = dbSet;

		if (predicate != null)
		{
			query = query.Where(predicate);
		}

		if (orderBy != null)
		{
			query = orderBy(query);
		}

		return await query.FirstOrDefaultAsync(cancellationToken);
	}

	public Task<int> CountAsync(CancellationToken cancellationToken)
	{
		return CountAsync(null, cancellationToken);
	}

	public async Task<int> CountAsync(Expression<Func<T, bool>>? predicate, CancellationToken cancellationToken)
	{
		if (predicate == null)
		{
			return await dbSet.CountAsync(cancellationToken);
		}

		return await dbSet.CountAsync(predicate, cancellationToken);
	}

	public async Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken)
	{
		return await dbSet.AnyAsync(predicate, cancellationToken);
	}
}