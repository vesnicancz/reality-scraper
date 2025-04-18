using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RealityScraper.Domain.Entities.Configuration;

namespace RealityScraper.Persistence.Configurations.Configuration;

public class ScraperRecipientConfiguration : IEntityTypeConfiguration<ScraperRecipient>
{
	public void Configure(EntityTypeBuilder<ScraperRecipient> builder)
	{
		builder.HasKey(e => e.Id);

		builder.Property(e => e.Email)
			.IsRequired()
			.HasMaxLength(100);
	}
}