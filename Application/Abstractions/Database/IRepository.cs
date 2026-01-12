using System.Linq.Expressions;
using RealityScraper.SharedKernel;

namespace RealityScraper.Application.Abstractions.Database;

public interface IRepository<T>
	where T : Entity
{
	void Add(T entity);

	//Task<T> AddAsync(T entity, CancellationToken cancellationToken);

	void Update(T entity);

	void Delete(T entity);

	Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

	Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken);

	Task<IReadOnlyList<T>> GetAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken);

	Task<IReadOnlyList<T>> GetAsync(
		Expression<Func<T, bool>>? predicate = null,
		Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
#pragma warning disable CA1002 // Do not expose generic lists
		List<Expression<Func<T, object>>>? includes = null,
#pragma warning restore CA1002 // Do not expose generic lists
		bool disableTracking = true,
		CancellationToken cancellationToken = default); // TODO: PM cancellation token s default?

	Task<T?> GetFirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken);

	Task<T?> GetFirstOrDefaultAsync(
		Expression<Func<T, bool>>? predicate,
		Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy,
		CancellationToken cancellationToken = default);

	Task<int> CountAsync(CancellationToken cancellationToken);

	Task<int> CountAsync(Expression<Func<T, bool>>? predicate, CancellationToken cancellationToken);

	Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken);
}