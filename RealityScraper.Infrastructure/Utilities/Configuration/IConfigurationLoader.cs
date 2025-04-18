namespace RealityScraper.Infrastructure.Utilities.Configuration;

public interface IConfigurationLoader
{
	Task<List<TaskConfiguration>> LoadConfigurationAsync(CancellationToken cancellationToken);
}