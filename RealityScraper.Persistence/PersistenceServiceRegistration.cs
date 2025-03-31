using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RealityScraper.Application.Interfaces;
using RealityScraper.Application.Interfaces.Repositories;
using RealityScraper.Application.Interfaces.Repositories.Realty;
using RealityScraper.Persistence.Contexts;
using RealityScraper.Persistence.Repositories;
using RealityScraper.Persistence.Repositories.Realty;

namespace RealityScraper.Persistence;

public static class PersistenceServiceRegistration
{
	public static IServiceCollection AddPersistenceServices(
		this IServiceCollection services,
		IConfiguration configuration)
	{
		// Konfigurace databázového kontextu
		services.AddDbContext<RealityDbContext>(options =>
			options.UseSqlite(
				configuration.GetConnectionString("DefaultConnection"),
				b => b.MigrationsAssembly(typeof(RealityDbContext).Assembly)
			)
		);

		// Registrace kontextu jako IDbContext
		services.AddScoped<IDbContext, RealityDbContext>();
		services.AddScoped<IUnitOfWork, UnitOfWork>();

		// Registrace repozitářů
		services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
		services.AddScoped<IListingRepository, ListingRepository>();

		return services;
	}
}