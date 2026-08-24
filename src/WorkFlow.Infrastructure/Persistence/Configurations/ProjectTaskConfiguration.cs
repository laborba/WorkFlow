using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkFlow.Domain.Entities;

namespace WorkFlow.Infrastructure.Persistence.Configurations;

public sealed class ProjectTaskConfiguration :
    IEntityTypeConfiguration<ProjectTask>
{
    public void Configure(
        EntityTypeBuilder<ProjectTask> builder)
    {
        builder.ToTable("project_tasks");

        builder.HasKey(task => task.Id)
            .HasName("pk_project_tasks");

        builder.Property(task => task.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(task => task.PublicId)
            .HasColumnName("public_id")
            .ValueGeneratedNever()
            .IsRequired();

        builder.Property(task => task.ProjectId)
            .HasColumnName("project_id")
            .IsRequired();

        builder.Property(task => task.Title)
            .HasColumnName("title")
            .IsRequired();

        builder.Property(task => task.Description)
            .HasColumnName("description");

        builder.Property(task => task.Status)
            .HasColumnName("status")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(task => task.Priority)
            .HasColumnName("priority")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(task => task.StatusBeforePause)
            .HasColumnName("status_before_pause")
            .HasConversion<int>();

        builder.Property(task => task.ResponsibleUserId)
            .HasColumnName("responsible_user_id");

        builder.Property(task => task.ValidatorUserId)
            .HasColumnName("validator_user_id");

        builder.Property(task => task.DueDate)
            .HasColumnName("due_date")
            .HasColumnType("timestamp with time zone");

        builder.Property(task => task.CreatedByUserId)
            .HasColumnName("created_by_user_id")
            .IsRequired();

        builder.Property(task => task.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(task => task.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(task => task.ArchivedAt)
            .HasColumnName("archived_at")
            .HasColumnType("timestamp with time zone");

        builder.Ignore(task => task.IsArchived);

        builder.HasIndex(task => task.PublicId)
            .IsUnique()
            .HasDatabaseName("ux_project_tasks_public_id");

        builder.HasIndex(
                task => new
                {
                    task.ProjectId,
                    task.Status
                })
            .HasDatabaseName(
                "ix_project_tasks_project_id_status");

        builder.HasIndex(
                task => new
                {
                    task.ProjectId,
                    task.DueDate
                })
            .HasDatabaseName(
                "ix_project_tasks_project_id_due_date");

        builder.HasIndex(
                task => new
                {
                    task.ResponsibleUserId,
                    task.Status
                })
            .HasDatabaseName(
                "ix_project_tasks_responsible_status");

        builder.HasIndex(
                task => new
                {
                    task.ValidatorUserId,
                    task.Status
                })
            .HasDatabaseName(
                "ix_project_tasks_validator_status");

        builder.HasIndex(task => task.CreatedByUserId)
            .HasDatabaseName(
                "ix_project_tasks_created_by_user_id");

        builder.HasOne<Project>()
            .WithMany()
            .HasForeignKey(task => task.ProjectId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName(
                "fk_project_tasks_projects");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(task => task.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName(
                "fk_project_tasks_created_by_users");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(task => task.ResponsibleUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName(
                "fk_project_tasks_responsible_users");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(task => task.ValidatorUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName(
                "fk_project_tasks_validator_users");
    }
}