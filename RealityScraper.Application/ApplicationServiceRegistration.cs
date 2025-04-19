using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RealityScraper.Application.Configuration;
using RealityScraper.Application.Features.Scraping;
using RealityScraper.Application.Features.Scraping.Scrapers;
using RealityScraper.Application.Interfaces.Mailing;
using RealityScraper.Application.Interfaces.Scraping;
using RealityScraper.Application.Services.Mailing;

namespace RealityScraper.Application;

public static class DependencyInjection
{
	public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
	{
		//// Registrace MediatR
		//services.AddMediatR(cfg =>
		//{
		//	cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);

		//	// Registrace chování
		//	cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
		//	cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
		//});

		//// Registrace AutoMapperu
		//services.AddAutoMapper(typeof(DependencyInjection).Assembly);

		//// Registrace validátorů
		//services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

		// konfigurace
		services.Configure<RealityIdnesScraperOptions>(configuration.GetSection("RealityIdnesScraper"));
		services.Configure<SRealityScraperOptions>(configuration.GetSection("SRealityScraper"));

		// registrace úloh
		services.AddTransient<ScraperServiceTask>();

		// scrapers
		services.AddTransient<IRealityScraperService, SRealityScraperService>();
		services.AddTransient<IRealityScraperService, RealityIdnesScraperService>();

		// mailing
		services.AddTransient<IMailerService, MailerService>();

		return services;
	}
}