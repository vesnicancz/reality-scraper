using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RealityScraper.Domain.Entities.Tasks;

namespace RealityScraper.Infrastructure.Database.Configurations.Tasks;

public class ReportTaskRecipientConfiguration : IEntityTypeConfiguration<ReportTaskRecipient>
{
	public void Configure(EntityTypeBuilder<ReportTaskRecipient> builder)
	{
		builder.HasKey(e => e.Id);

		builder.Property(e => e.Email)
			.IsRequired()
			.HasMaxLength(100);
	}
}
