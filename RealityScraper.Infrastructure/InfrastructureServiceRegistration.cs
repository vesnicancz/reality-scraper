using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RealityScraper.Application.Features.Scheduling.Configuration;
using RealityScraper.Application.Interfaces.Mailing;
using RealityScraper.Infrastructure.BackgroundServices.Scheduler;
using RealityScraper.Infrastructure.Utilities.Mailing;

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

		services.AddHostedService<SchedulerHostedService>();

		return services;
	}
}