using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RealityScraper.Domain.Entities.Tasks;

namespace RealityScraper.Infrastructure.Database.Configurations.Tasks;

public class ScraperTaskRecipientConfiguration : IEntityTypeConfiguration<ScraperTaskRecipient>
{
	public void Configure(EntityTypeBuilder<ScraperTaskRecipient> builder)
	{
		builder.HasKey(e => e.Id);

		builder.Property(e => e.Email)
			.IsRequired()
			.HasMaxLength(100);
	}
}