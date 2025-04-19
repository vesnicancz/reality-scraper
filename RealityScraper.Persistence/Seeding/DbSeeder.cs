using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RealityScraper.Domain.Entities.Configuration;
using RealityScraper.Persistence.Contexts;
using RealityScraper.Persistence.Seeding.Configuration;

namespace RealityScraper.Persistence.Seeding;

public static class DbSeeder
{
	public static async Task SeedTasksFromConfigurationAsync(IHost host)
	{
		using var scope = host.Services.CreateScope();
		var services = scope.ServiceProvider;

		try
		{
			var context = services.GetRequiredService<RealityDbContext>();
			var configuration = services.GetRequiredService<IConfiguration>();
			var logger = services.GetRequiredService<ILogger<RealityDbContext>>();

			await SeedTasksAsync(context, configuration, logger, CancellationToken.None);
		}
		catch (Exception ex)
		{
			var logger = services.GetRequiredService<ILogger<RealityDbContext>>();
			logger.LogError(ex, "An error occurred while seeding the database.");
		}
	}

	private static async Task SeedTasksAsync(RealityDbContext context, IConfiguration configuration, ILogger logger, CancellationToken cancellationToken)
	{
		// Kontrola, zda jsou v databázi již nějaké úlohy
		if (await context.Set<ScraperTask>().AnyAsync())
		{
			logger.LogTrace("Tasks already exist in the database. Skipping seeding.");
			return;
		}

		// Načtení úloh z konfigurace
		var schedulerSettings = configuration.GetSection("SchedulerSettings").Get<SchedulerSettings>();
		if (schedulerSettings?.Tasks == null || !schedulerSettings.Tasks.Any())
		{
			logger.LogTrace("No tasks found in configuration. Skipping seeding.");
			return;
		}

		// Převod konfiguračních úloh na databázové entity
		var tasks = new List<ScraperTask>();

		foreach (var taskConfig in schedulerSettings.Tasks)
		{
			var task = new ScraperTask
			{
				Name = taskConfig.Name,
				CronExpression = taskConfig.CronExpression,
				Enabled = taskConfig.Enabled,
				CreatedAt = DateTime.UtcNow,
				Recipients = taskConfig.ScrapingConfiguration.EmailRecipients
					.Select(email => new ScraperRecipient { Email = email })
					.ToList(),
				Targets = taskConfig.ScrapingConfiguration.Scrapers
					.Select(scraper => new ScraperTaskTarget
					{
						ScraperType = scraper.ScraperType,
						Url = scraper.Url
					})
					.ToList()
			};

			tasks.Add(task);
		}

		// Uložení úloh do databáze
		await context.Set<ScraperTask>().AddRangeAsync(tasks);
		await context.SaveChangesAsync();

		logger.LogTrace("Successfully seeded {count} tasks from configuration.", tasks.Count);
	}
}