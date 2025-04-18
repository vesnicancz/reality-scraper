using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RealityScraper.Domain.Entities.Configuration;

namespace RealityScraper.Persistence.Configurations.Configuration;

public class ScraperTaskTargetConfiguration : IEntityTypeConfiguration<ScraperTaskTarget>
{
	public void Configure(EntityTypeBuilder<ScraperTaskTarget> builder)
	{
		builder.HasKey(e => e.Id);

		builder.Property(e => e.Url)
			.IsRequired()
			.HasMaxLength(500);
	}
}