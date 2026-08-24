using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkFlow.Domain.Entities;

namespace WorkFlow.Infrastructure.Persistence.Configurations;

public sealed class ProjectMemberConfiguration :
    IEntityTypeConfiguration<ProjectMember>
{
    public void Configure(
        EntityTypeBuilder<ProjectMember> builder)
    {
        builder.ToTable("project_members");

        builder.HasKey(member => member.Id)
            .HasName("pk_project_members");

        builder.Property(member => member.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(member => member.ProjectId)
            .HasColumnName("project_id")
            .IsRequired();

        builder.Property(member => member.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(member => member.AddedByUserId)
            .HasColumnName("added_by_user_id")
            .IsRequired();

        builder.Property(member => member.AddedAt)
            .HasColumnName("added_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(member => member.RemovedAt)
            .HasColumnName("removed_at")
            .HasColumnType("timestamp with time zone");

        builder.HasIndex(
                member => new
                {
                    member.ProjectId,
                    member.UserId
                })
            .IsUnique()
            .HasFilter("removed_at IS NULL")
            .HasDatabaseName(
                "ux_project_members_active");

        builder.HasIndex(member => member.UserId)
            .HasDatabaseName(
                "ix_project_members_user_id");

        builder.HasIndex(member => member.AddedByUserId)
            .HasDatabaseName(
                "ix_project_members_added_by_user_id");

        builder.HasOne<Project>()
            .WithMany()
            .HasForeignKey(member => member.ProjectId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName(
                "fk_project_members_projects");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(member => member.UserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName(
                "fk_project_members_users");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(member => member.AddedByUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName(
                "fk_project_members_added_by_users");
    }
}