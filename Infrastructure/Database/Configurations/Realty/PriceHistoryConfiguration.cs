using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RealityScraper.Domain.Entities.Realty;

namespace RealityScraper.Infrastructure.Configurations.Realty;

public class PriceHistoryConfiguration : IEntityTypeConfiguration<PriceHistory>
{
	public void Configure(EntityTypeBuilder<PriceHistory> builder)
	{
		builder.HasKey(e => e.Id);
	}
}