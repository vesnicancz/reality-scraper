using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RealityScraper.Application.Features.Scheduling.Configuration;
using RealityScraper.Application.Features.Scraping.Scrapers;
using RealityScraper.Application.Interfaces.Mailing;
using RealityScraper.Application.Interfaces.Scraping;
using RealityScraper.Infrastructure.BackgroundServices.Scheduler;
using RealityScraper.Infrastructure.Utilities;
using RealityScraper.Infrastructure.Utilities.Mailing;
using RealityScraper.Infrastructure.Utilities.Scraping;

namespace RealityScraper.Infrastructure;

public static class InfrastructureServiceRegistration
{
	public static IServiceCollection AddInfrastructureServices(
		this IServiceCollection services,
		IConfiguration configuration)
	{
		services.AddHttpClient();

		services.Configure<SchedulerSettings>(configuration.GetSection("SchedulerSettings"));

		// mail settings
		services.AddTransient<IEmailService, SendGridEmailService>();
		//services.AddTransient<IEmailService, SmtpEmailService>();

		services.AddTransient<IEmailGenerator, RazorEmailGenerator>();
		//services.AddTransient<IEmailGenerator, HtmlEmailGenerator>();

		services.AddTransient<IImageDownloadService, ImageDownloadService>();
		services.AddTransient<IWebDriverFactory, ChromeDriverFactory>();

		// scrapers
		services.AddTransient<IRealityScraperService, SRealityScraperService>();
		services.AddTransient<IRealityScraperService, RealityIdnesScraperService>();

		services.AddHostedService<SchedulerHostedService>();

		return services;
	}
}