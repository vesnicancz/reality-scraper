using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RealityScraper.Domain.Entities.Configuration;
using RealityScraper.Domain.Entities.Realty;

namespace RealityScraper.Persistence.Configurations.Realty;

public class ListingConfiguration : IEntityTypeConfiguration<Listing>
{
	public void Configure(EntityTypeBuilder<Listing> builder)
	{
		builder.HasKey(e => e.Id);

		builder.HasIndex(e => new { e.ExternalId, e.ScraperTaskId }) // TODO: ScraperType?
			.IsUnique();

		builder.HasMany(l => l.PriceHistories)
			.WithOne(p => p.Listing)
			.HasForeignKey(p => p.ListingId);

		// Configure only the foreign key relationship without navigation property
		builder.Property(e => e.ScraperTaskId)
			.IsRequired(false);

		// Create the relationship without exposing navigation properties
		builder.HasOne<ScraperTask>()  // Note: no navigation property specified
			.WithMany()  // No collection navigation property on ScraperTask
			.HasForeignKey(e => e.ScraperTaskId)
			.IsRequired(false)
			.OnDelete(DeleteBehavior.Cascade);
	}
}