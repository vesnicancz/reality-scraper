﻿using Microsoft.Extensions.DependencyInjection;
using RealityScraper.Application.Features.Scraping;
using RealityScraper.Application.Features.Scraping.Scrapers;
using RealityScraper.Application.Interfaces.Mailing;
using RealityScraper.Application.Services.Mailing;

namespace RealityScraper.Application;

public static class DependencyInjection
{
	public static IServiceCollection AddApplicationServices(this IServiceCollection services)
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

		// registrace úloh
		services.AddTransient<ScraperServiceTask>();

		// mailing
		services.AddTransient<IMailerService, MailerService>();

		return services;
	}
}