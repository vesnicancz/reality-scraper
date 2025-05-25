using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RazorEngineCore;
using RealityScraper.Application.Interfaces.Mailing;
using RealityScraper.Application.Interfaces.Scraping;
using RealityScraper.Infrastructure.BackgroundServices.Scheduler;
using RealityScraper.Infrastructure.Configuration;
using RealityScraper.Infrastructure.Utilities;
using RealityScraper.Infrastructure.Utilities.Mailing;
using RealityScraper.Infrastructure.Utilities.Scheduler;
using RealityScraper.Infrastructure.Utilities.WebDriver;

namespace RealityScraper.Infrastructure;

public static class InfrastructureServiceRegistration
{
	public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
	{
		// configuration
		services.Configure<SmtpOptions>(configuration.GetSection("SmtpSettings"));
		services.Configure<SendGridOptions>(configuration.GetSection("SendGridSettings"));
		services.Configure<SeleniumOptions>(configuration.GetSection("SeleniumSettings"));

		services.AddHttpClient();

		//services.Configure<SchedulerSettings>(configuration.GetSection("SchedulerSettings"));

		// mail settings
		services.AddTransient<IEmailService, SendGridEmailService>();
		//services.AddTransient<IEmailService, SmtpEmailService>();

		services.AddTransient<IRazorEngine, RazorEngine>();
		services.AddTransient<IEmailGenerator, RazorEmailGenerator>();
		//services.AddTransient<IEmailGenerator, HtmlEmailGenerator>();

		services.AddTransient<IImageDownloadService, ImageDownloadService>();
		services.AddTransient<IWebDriverFactory, SeleniumChromeDriverFactory>();

		services.AddTransient<IScheduleTimeCalculator, CronosScheduleTimeCalculator>();
		services.AddTransient<ITaskSchedulerService, TaskSchedulerService>();

		services.AddHostedService<SchedulerHostedService>();

		return services;
	}
}