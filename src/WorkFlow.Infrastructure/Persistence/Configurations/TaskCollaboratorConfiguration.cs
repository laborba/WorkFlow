using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkFlow.Domain.Entities;

namespace WorkFlow.Infrastructure.Persistence.Configurations;

public sealed class TaskCollaboratorConfiguration
    : IEntityTypeConfiguration<TaskCollaborator>
{
    public void Configure(EntityTypeBuilder<TaskCollaborator> builder)
    {
        builder.ToTable("task_collaborators");

        builder.HasKey(taskCollaborator => taskCollaborator.Id)
            .HasName("pk_task_collaborators");

        builder.Property(taskCollaborator => taskCollaborator.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(taskCollaborator => taskCollaborator.TaskId)
            .HasColumnName("task_id")
            .IsRequired();

        builder.Property(taskCollaborator => taskCollaborator.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.HasIndex(taskCollaborator =>
            taskCollaborator.AddedByUserId)
        .HasDatabaseName(
            "ix_task_collaborators_added_by_user_id");

        builder.HasIndex(taskCollaborator =>
            taskCollaborator.RemovedByUserId)
        .HasDatabaseName(
            "ix_task_collaborators_removed_by_user_id");

        builder.Property(taskCollaborator => taskCollaborator.AddedAt)
            .HasColumnName("added_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(taskCollaborator => taskCollaborator.AddedByUserId)
            .HasColumnName("added_by_user_id")
            .IsRequired();

        builder.Property(taskCollaborator => taskCollaborator.RemovedAt)
            .HasColumnName("removed_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(taskCollaborator => taskCollaborator.RemovedByUserId)
            .HasColumnName("removed_by_user_id");

        builder.Ignore(taskCollaborator => taskCollaborator.IsActive);

        builder.HasIndex(taskCollaborator => taskCollaborator.TaskId)
            .HasDatabaseName("ix_task_collaborators_task_id");

        builder.HasIndex(taskCollaborator => taskCollaborator.UserId)
            .HasDatabaseName("ix_task_collaborators_user_id");

        builder.HasIndex(taskCollaborator => new
        {
            taskCollaborator.TaskId,
            taskCollaborator.UserId
        })
            .IsUnique()
            .HasFilter("removed_at IS NULL")
            .HasDatabaseName(
                "ux_task_collaborators_task_id_user_id_active");

        builder.HasOne<ProjectTask>()
            .WithMany()
            .HasForeignKey(taskCollaborator => taskCollaborator.TaskId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName(
                "fk_task_collaborators_project_tasks_task_id");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(taskCollaborator => taskCollaborator.UserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName(
                "fk_task_collaborators_users_user_id");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(taskCollaborator =>
                taskCollaborator.AddedByUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName(
                "fk_task_collaborators_users_added_by_user_id");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(taskCollaborator =>
                taskCollaborator.RemovedByUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName(
                "fk_task_collaborators_users_removed_by_user_id");
    }
}