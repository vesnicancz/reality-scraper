using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RealityScraper.Domain.Entities.Realty;

namespace RealityScraper.Persistence.Configurations;

public class ListingConfiguration : IEntityTypeConfiguration<Listing>
{
	public void Configure(EntityTypeBuilder<Listing> builder)
	{
		builder.HasKey(e => e.Id);

		builder.HasIndex(e => e.ExternalId);

		builder.HasMany(l => l.PriceHistories)
			.WithOne(p => p.Listing)
			.HasForeignKey(p => p.ListingId);
	}
}