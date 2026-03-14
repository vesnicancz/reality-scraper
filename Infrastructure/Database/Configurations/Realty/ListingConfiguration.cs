using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RealityScraper.Domain.Entities.Realty;
using RealityScraper.Domain.Entities.Tasks;

namespace RealityScraper.Infrastructure.Configurations.Realty;

public class ListingConfiguration : IEntityTypeConfiguration<Listing>
{
	public void Configure(EntityTypeBuilder<Listing> builder)
	{
		builder.HasKey(e => e.Id);

		builder.Property(e => e.ExternalId)
			.HasMaxLength(100);

		builder.Property(e => e.Title)
			.HasMaxLength(300);

		builder.Property(e => e.Location)
			.HasMaxLength(300);

		builder.Property(e => e.Url)
			.HasMaxLength(500);

		builder.Property(e => e.ImageUrl)
			.HasMaxLength(500);

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