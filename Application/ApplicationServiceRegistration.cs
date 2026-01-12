using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RealityScraper.Application.Abstractions.Behaviors;
using RealityScraper.Application.Abstractions.Messaging;
using RealityScraper.Application.Configuration;
using RealityScraper.Application.Features.Scheduler;
using RealityScraper.Application.Features.Scraping;
using RealityScraper.Application.Features.Scraping.Builders;
using RealityScraper.Application.Features.Scraping.Scrapers;
using RealityScraper.Application.Interfaces.Mailing;
using RealityScraper.Application.Interfaces.Scheduler;
using RealityScraper.Application.Interfaces.Scraping;
using RealityScraper.Application.Services.Mailing;
using RealityScraper.SharedKernel;

namespace RealityScraper.Application;

public static class DependencyInjection
{
	public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
	{
		RegisterMessaging(services);
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

		services.AddTransient<ScrapingReportBuilder>();

		// registrace úloh
		services.AddTransient<ScraperServiceTask>();

		// scrapers
		services.AddTransient<IRealityScraperService, SRealityScraperService>();
		services.AddTransient<IRealityScraperService, RealityIdnesScraperService>();

		services.AddTransient<IScrapingReportProcessor, ScrapingReportProcessor>();

		// task scheduler
		services.AddTransient<ITaskSchedulerService, TaskSchedulerService>();

		// mailing
		services.AddTransient<IMailerService, MailerService>();

		return services;
	}

	private static IServiceCollection RegisterMessaging(IServiceCollection services)
	{
		// Register application services here
		services.Scan(scan => scan.FromAssembliesOf(typeof(DependencyInjection))
			.AddClasses(classes => classes.AssignableTo(typeof(IQueryHandler<,>)), publicOnly: false)
				.AsImplementedInterfaces()
				.WithScopedLifetime()
			.AddClasses(classes => classes.AssignableTo(typeof(ICommandHandler<>)), publicOnly: false)
				.AsImplementedInterfaces()
				.WithScopedLifetime()
			.AddClasses(classes => classes.AssignableTo(typeof(ICommandHandler<,>)), publicOnly: false)
				.AsImplementedInterfaces()
				.WithScopedLifetime());

		services.TryDecorate(typeof(ICommandHandler<,>), typeof(ValidationDecorator.CommandHandler<,>));
		services.TryDecorate(typeof(ICommandHandler<>), typeof(ValidationDecorator.CommandBaseHandler<>));

		services.TryDecorate(typeof(IQueryHandler<,>), typeof(LoggingDecorator.QueryHandler<,>));
		services.TryDecorate(typeof(ICommandHandler<,>), typeof(LoggingDecorator.CommandHandler<,>));
		services.TryDecorate(typeof(ICommandHandler<>), typeof(LoggingDecorator.CommandBaseHandler<>));

		services.Scan(scan => scan.FromAssembliesOf(typeof(DependencyInjection))
			.AddClasses(classes => classes.AssignableTo(typeof(IDomainEventHandler<>)), publicOnly: false)
			.AsImplementedInterfaces()
			.WithScopedLifetime());

		services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, includeInternalTypes: true);

		return services;
	}
}