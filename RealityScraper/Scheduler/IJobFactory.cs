namespace RealityScraper.Scheduler;

// Rozhraní pro továrnu úloh
public interface IJobFactory
{
	IJob Create<T>()
		where T : IJob;
}