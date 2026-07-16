using System.Linq.Expressions;
using RealityScraper.SharedKernel;

namespace RealityScraper.Application.Abstractions.Database;

public interface IRepository<T>
	where T : Entity
{
	void Add(T entity);

	void Delete(T entity);

	Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

	Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken);

	Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken);
}
