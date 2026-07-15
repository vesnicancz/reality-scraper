using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RealityScraper.Domain.Entities.Tasks;

namespace RealityScraper.Infrastructure.Database.Configurations.Tasks;

public class RemovedListingsReportTaskConfiguration : IEntityTypeConfiguration<RemovedListingsReportTask>
{
	public void Configure(EntityTypeBuilder<RemovedListingsReportTask> builder)
	{
		builder.HasMany(e => e.Recipients)
			.WithOne(e => e.ReportTask)
			.HasForeignKey(e => e.ReportTaskId)
			.OnDelete(DeleteBehavior.Cascade);

		builder.Navigation(e => e.Recipients)
			.UsePropertyAccessMode(PropertyAccessMode.Field);

		builder.HasMany(e => e.Sources)
			.WithOne(e => e.ReportTask)
			.HasForeignKey(e => e.ReportTaskId)
			.OnDelete(DeleteBehavior.Cascade);

		builder.Navigation(e => e.Sources)
			.UsePropertyAccessMode(PropertyAccessMode.Field);
	}
}
