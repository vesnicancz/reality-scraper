using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RealityScraper.Domain.Entities.Tasks;

namespace RealityScraper.Infrastructure.Database.Configurations.Tasks;

public class ScraperTaskConfiguration : IEntityTypeConfiguration<ScraperTask>
{
	public void Configure(EntityTypeBuilder<ScraperTask> builder)
	{
		builder.HasMany(e => e.Recipients)
			.WithOne(e => e.ScraperTask)
			.HasForeignKey(e => e.ScraperTaskId)
			.OnDelete(DeleteBehavior.Cascade);

		builder.Navigation(e => e.Recipients)
			.UsePropertyAccessMode(PropertyAccessMode.Field);

		builder.HasMany(e => e.Targets)
			.WithOne(e => e.ScraperTask)
			.HasForeignKey(e => e.ScraperTaskId)
			.OnDelete(DeleteBehavior.Cascade);

		builder.Navigation(e => e.Targets)
			.UsePropertyAccessMode(PropertyAccessMode.Field);
	}
}