namespace RealityScraper.Scheduler;

// Implementace továrny úloh s využitím dependency injection
public class JobFactory : IJobFactory
{
	private readonly IServiceProvider serviceProvider;

	public JobFactory(IServiceProvider serviceProvider)
	{
		this.serviceProvider = serviceProvider;
	}

	public IJob Create<T>()
		where T : IJob
	{
		return serviceProvider.GetRequiredService<T>();
	}
}