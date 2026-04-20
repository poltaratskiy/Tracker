using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Tracker.Dotnet.Tasks.Persistence.Configurations;

public class TasksConfiguration : IEntityTypeConfiguration<Domain.Entities.TaskEntity>
{
    public void Configure(EntityTypeBuilder<Domain.Entities.TaskEntity> builder)
    {
        builder.ToTable("Tasks");

        builder.HasKey(t => t.Id);

        builder.HasOne(x => x.Assignee)
            .WithMany()
            .HasForeignKey(x => x.AssigneeId);
    }
}
