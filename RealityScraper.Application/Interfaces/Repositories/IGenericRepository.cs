using System.Linq.Expressions;
using RealityScraper.Domain.Common;

namespace RealityScraper.Application.Interfaces.Repositories;

public interface IRepository<T> where T : BaseEntity
{
	Task<T> GetByIdAsync(int id, CancellationToken cancellationToken);

	Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken);

	Task<IReadOnlyList<T>> GetAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken);

	//Task<IReadOnlyList<T>> GetAsync(
	//	Expression<Func<T, bool>> predicate = null,
	//	Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null,
	//	string includeString = null,
	//	bool disableTracking = true);

	// TODO: cancellation token
	Task<IReadOnlyList<T>> GetAsync(
		Expression<Func<T, bool>>? predicate = null,
		Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
		List<Expression<Func<T, object>>>? includes = null,
		bool disableTracking = true);

	Task<T> AddAsync(T entity, CancellationToken cancellationToken);

	void Update(T entity);

	void Delete(T entity);

	Task<int> CountAsync(CancellationToken cancellationToken);

	Task<int> CountAsync(Expression<Func<T, bool>>? predicate, CancellationToken cancellationToken);

	Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken);
}