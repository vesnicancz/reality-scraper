using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using RealityScraper.Application.Interfaces.Repositories;
using RealityScraper.Domain.Common;
using RealityScraper.Persistence.Contexts;

namespace RealityScraper.Persistence.Repositories;

public class Repository<T> : IRepository<T>
	where T : BaseEntity
{
	protected readonly IDbContext dbContext;
	protected readonly DbSet<T> dbSet;

	public Repository(IDbContext dbContext)
	{
		this.dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
		dbSet = dbContext.Set<T>();
	}

	public async Task<T> GetByIdAsync(int id, CancellationToken cancellationToken)
	{
		return await dbSet.FindAsync(id, cancellationToken);
	}

	public async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken)
	{
		return await dbSet.ToListAsync(cancellationToken);
	}

	public async Task<IReadOnlyList<T>> GetAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken)
	{
		return await dbSet.Where(predicate).ToListAsync(cancellationToken);
	}

	//public async Task<IReadOnlyList<T>> GetAsync(
	//	Expression<Func<T, bool>> predicate = null,
	//	Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null,
	//	string includeString = null,
	//	bool disableTracking = true)
	//{
	//	IQueryable<T> query = _dbSet;

	//	if (disableTracking)
	//		query = query.AsNoTracking();

	//	if (!string.IsNullOrWhiteSpace(includeString))
	//		query = query.Include(includeString);

	//	if (predicate != null)
	//		query = query.Where(predicate);

	//	if (orderBy != null)
	//		return await orderBy(query).ToListAsync();

	//	return await query.ToListAsync();
	//}

	public async Task<IReadOnlyList<T>> GetAsync(
		Expression<Func<T, bool>>? predicate = null,
		Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
		List<Expression<Func<T, object>>>? includes = null,
		bool disableTracking = true)
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
			return await orderBy(query).ToListAsync();
		}

		return await query.ToListAsync();
	}

	public async Task<T> AddAsync(T entity, CancellationToken cancellationToken)
	{
		await dbSet.AddAsync(entity, cancellationToken);
		return entity;
	}

	public void Update(T entity)
	{
		dbSet.Attach(entity);
		dbContext.Entry(entity).State = EntityState.Modified;
	}

	public void Delete(T entity)
	{
		dbSet.Remove(entity);
	}

	public Task<int> CountAsync(CancellationToken cancellationToken)
	{
		return CountAsync(null, cancellationToken);
	}

	public async Task<int> CountAsync(Expression<Func<T, bool>>? predicate, CancellationToken cancellationToken)
	{
		if (predicate == null)
		{
			return await dbSet.CountAsync();
		}

		return await dbSet.CountAsync(predicate);
	}

	public async Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken)
	{
		return await dbSet.AnyAsync(predicate, cancellationToken);
	}
}