using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RealityScraper.Domain.Entities.Tasks;

namespace RealityScraper.Infrastructure.Database.Configurations.Tasks;

public class ReportTaskSourceConfiguration : IEntityTypeConfiguration<ReportTaskSource>
{
	public void Configure(EntityTypeBuilder<ReportTaskSource> builder)
	{
		builder.HasKey(e => e.Id);

		builder.HasIndex(e => new { e.ReportTaskId, e.ScraperTaskId })
			.IsUnique();

		builder.HasOne(e => e.ScraperTask)
			.WithMany()
			.HasForeignKey(e => e.ScraperTaskId)
			.OnDelete(DeleteBehavior.Cascade);
	}
}
