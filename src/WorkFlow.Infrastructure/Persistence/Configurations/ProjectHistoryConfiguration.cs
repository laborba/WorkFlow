using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkFlow.Domain.Entities;

namespace WorkFlow.Infrastructure.Persistence.Configurations;

public sealed class ProjectHistoryConfiguration
    : IEntityTypeConfiguration<ProjectHistory>
{
    public void Configure(EntityTypeBuilder<ProjectHistory> builder)
    {
        builder.ToTable("project_histories");

        builder.HasKey(projectHistory => projectHistory.Id)
            .HasName("pk_project_histories");

        builder.Property(projectHistory => projectHistory.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(projectHistory => projectHistory.ProjectId)
            .HasColumnName("project_id")
            .IsRequired();

        builder.Property(projectHistory => projectHistory.ActorUserId)
            .HasColumnName("actor_user_id")
            .IsRequired();

        builder.Property(projectHistory => projectHistory.Action)
            .HasColumnName("action")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(projectHistory => projectHistory.Reason)
            .HasColumnName("reason")
            .HasColumnType("text");

        builder.Property(projectHistory => projectHistory.OldValue)
            .HasColumnName("old_value")
            .HasColumnType("text");

        builder.Property(projectHistory => projectHistory.NewValue)
            .HasColumnName("new_value")
            .HasColumnType("text");

        builder.Property(projectHistory => projectHistory.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasIndex(projectHistory => new
        {
            projectHistory.ProjectId,
            projectHistory.CreatedAt
        })
            .HasDatabaseName(
                "ix_project_histories_project_id_created_at");

        builder.HasIndex(projectHistory => projectHistory.ActorUserId)
            .HasDatabaseName("ix_project_histories_actor_user_id");

        builder.HasOne<Project>()
            .WithMany()
            .HasForeignKey(projectHistory => projectHistory.ProjectId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName(
                "fk_project_histories_projects_project_id");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(projectHistory =>
                projectHistory.ActorUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName(
                "fk_project_histories_users_actor_user_id");
    }
}