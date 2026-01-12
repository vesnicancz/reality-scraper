using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RazorEngineCore;
using RealityScraper.Application.Abstractions.Database;
using RealityScraper.Application.Interfaces.Mailing;
using RealityScraper.Application.Interfaces.Scheduler;
using RealityScraper.Application.Interfaces.Scraping;
using RealityScraper.Infrastructure.BackgroundServices.Scheduler;
using RealityScraper.Infrastructure.Configuration;
using RealityScraper.Infrastructure.Contexts;
using RealityScraper.Infrastructure.Database;
using RealityScraper.Infrastructure.Services.Time;
using RealityScraper.Infrastructure.Utilities;
using RealityScraper.Infrastructure.Utilities.Mailing;
using RealityScraper.Infrastructure.Utilities.Scheduler;
using RealityScraper.Infrastructure.Utilities.WebDriver;
using RealityScraper.SharedKernel;
using Resend;

namespace RealityScraper.Infrastructure;

public static class InfrastructureServiceRegistration
{
	public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
	{
		AddServices(services, configuration);
		AddDatabase(services, configuration);

		return services;
	}

	private static IServiceCollection AddServices(this IServiceCollection services, IConfiguration configuration)
	{
		// configuration
		services.Configure<SmtpOptions>(configuration.GetSection("SmtpSettings"));
		services.Configure<ResendOptions>(configuration.GetSection("ResendSettings"));
		services.Configure<SeleniumOptions>(configuration.GetSection("SeleniumSettings"));

		services.AddHttpClient();

		// mail settings
		//services.AddTransient<IEmailService, SmtpEmailService>();
		services.AddHttpClient<ResendClient>();
		services.Configure<ResendClientOptions>(o =>
		{
			o.ApiToken = configuration.GetValue<string>("ResendSettings:ApiKey");
		});
		services.AddTransient<IResend, ResendClient>();
		services.AddTransient<IEmailService, ResendEmailService>();

		services.AddTransient<IRazorEngine, RazorEngine>();
		services.AddTransient<IEmailGenerator, RazorEmailGenerator>();
		//services.AddTransient<IEmailGenerator, HtmlEmailGenerator>();

		services.AddTransient<IImageDownloadService, ImageDownloadService>();
		services.AddTransient<IWebDriverFactory, SeleniumChromeDriverFactory>();

		services.AddTransient<IScheduleTimeCalculator, CronosScheduleTimeCalculator>();

		services.AddHostedService<SchedulerHostedService>();

		services.AddSingleton<IDateTimeProvider, DateTimeProvider>();

		return services;
	}

	private static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
	{
		// Konfigurace databázového kontextu
		services.AddDbContext<RealityDbContext>(options =>
			options.UseNpgsql(
				configuration.GetConnectionString("DefaultConnection"),
				b => b.MigrationsAssembly(typeof(RealityDbContext).Assembly)
			)
		);

		services.AddScoped<IDbContext, RealityDbContext>();
		services.AddScoped<IUnitOfWork, UnitOfWork>();

		// všechny implementace IRepository<> v aktuální assembly
		services.Scan(scan => scan
			.FromAssembliesOf(typeof(InfrastructureServiceRegistration))
			.AddClasses(classes => classes.AssignableTo(typeof(IRepository<>)), false)
			.AsMatchingInterface()
			.WithScopedLifetime());

		return services;
	}
}