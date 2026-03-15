using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RealityScraper.Domain.Entities.Tasks;

namespace RealityScraper.Infrastructure.Database.Configurations.Tasks;

public class TaskBaseConfiguration : IEntityTypeConfiguration<TaskBase>
{
	public void Configure(EntityTypeBuilder<TaskBase> builder)
	{
		builder.ToTable("Tasks");

		builder.HasDiscriminator();

		builder.HasKey(e => e.Id);

		builder.Property(e => e.Name)
			.IsRequired()
			.HasMaxLength(100);

		builder.Property(e => e.CronExpression)
			.IsRequired()
			.HasMaxLength(50);

		builder.Property(e => e.LastRunLog)
			.HasColumnType("text");

		builder.Property(e => e.LastRunSucceeded);
	}
}