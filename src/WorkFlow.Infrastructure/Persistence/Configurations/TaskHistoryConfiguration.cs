using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkFlow.Domain.Entities;

namespace WorkFlow.Infrastructure.Persistence.Configurations;

public sealed class TaskHistoryConfiguration
    : IEntityTypeConfiguration<TaskHistory>
{
    public void Configure(EntityTypeBuilder<TaskHistory> builder)
    {
        builder.ToTable("task_histories");

        builder.HasKey(taskHistory => taskHistory.Id)
            .HasName("pk_task_histories");

        builder.Property(taskHistory => taskHistory.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(taskHistory => taskHistory.TaskId)
            .HasColumnName("task_id")
            .IsRequired();

        builder.Property(taskHistory => taskHistory.ActorUserId)
            .HasColumnName("actor_user_id")
            .IsRequired();

        builder.Property(taskHistory => taskHistory.Action)
            .HasColumnName("action")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(taskHistory => taskHistory.Reason)
            .HasColumnName("reason")
            .HasColumnType("text");

        builder.Property(taskHistory => taskHistory.OldValue)
            .HasColumnName("old_value")
            .HasColumnType("text");

        builder.Property(taskHistory => taskHistory.NewValue)
            .HasColumnName("new_value")
            .HasColumnType("text");

        builder.Property(taskHistory => taskHistory.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasIndex(taskHistory => new
        {
            taskHistory.TaskId,
            taskHistory.CreatedAt
        })
            .HasDatabaseName(
                "ix_task_histories_task_id_created_at");

        builder.HasIndex(taskHistory => taskHistory.ActorUserId)
            .HasDatabaseName("ix_task_histories_actor_user_id");

        builder.HasOne<ProjectTask>()
            .WithMany()
            .HasForeignKey(taskHistory => taskHistory.TaskId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName(
                "fk_task_histories_project_tasks_task_id");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(taskHistory =>
                taskHistory.ActorUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName(
                "fk_task_histories_users_actor_user_id");
    }
}