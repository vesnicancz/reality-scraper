using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RealityScraper.Domain.Entities.Configuration;

namespace RealityScraper.Persistence.Configurations.Configuration;

public class ScraperTaskConfiguration : IEntityTypeConfiguration<ScraperTask>
{
	public void Configure(EntityTypeBuilder<ScraperTask> builder)
	{
		builder.HasKey(e => e.Id);

		builder.Property(e => e.Name)
			.IsRequired()
			.HasMaxLength(100);

		builder.Property(e => e.CronExpression)
			.IsRequired()
			.HasMaxLength(50);

		builder.HasMany(e => e.Recipients)
			.WithOne(e => e.ScraperTask)
			.HasForeignKey(e => e.ScraperTaskId)
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasMany(e => e.Targets)
			.WithOne(e => e.ScraperTask)
			.HasForeignKey(e => e.ScraperTaskId)
			.OnDelete(DeleteBehavior.Cascade);
	}
}