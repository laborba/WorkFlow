using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkFlow.Domain.Entities;

namespace WorkFlow.Infrastructure.Persistence.Configurations;

public sealed class ProjectConfiguration :
    IEntityTypeConfiguration<Project>
{
    public void Configure(
        EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("projects");

        builder.HasKey(project => project.Id)
            .HasName("pk_projects");

        builder.Property(project => project.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(project => project.PublicId)
            .HasColumnName("public_id")
            .ValueGeneratedNever()
            .IsRequired();

        builder.Property(project => project.TenantId)
            .HasColumnName("tenant_id")
            .IsRequired();

        builder.Property(project => project.Name)
            .HasColumnName("name")
            .IsRequired();

        builder.Property(project => project.Description)
            .HasColumnName("description");

        builder.Property(project => project.ResponsibleUserId)
            .HasColumnName("responsible_user_id");

        builder.Property(project => project.CreatedByUserId)
            .HasColumnName("created_by_user_id")
            .IsRequired();

        builder.Property(project => project.Status)
            .HasColumnName("status")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(project => project.StatusBeforeArchive)
            .HasColumnName("status_before_archive")
            .HasConversion<int>();

        builder.Property(project => project.DueDate)
            .HasColumnName("due_date")
            .HasColumnType("timestamp with time zone");

        builder.Property(project => project.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(project => project.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(project => project.ArchivedAt)
            .HasColumnName("archived_at")
            .HasColumnType("timestamp with time zone");

        builder.HasIndex(project => project.PublicId)
            .IsUnique()
            .HasDatabaseName("ux_projects_public_id");

        builder.HasIndex(
                project => new
                {
                    project.TenantId,
                    project.Status
                })
            .HasDatabaseName(
                "ix_projects_tenant_id_status");

        builder.HasIndex(
                project => new
                {
                    project.TenantId,
                    project.DueDate
                })
            .HasDatabaseName(
                "ix_projects_tenant_id_due_date");

        builder.HasIndex(project => project.ResponsibleUserId)
            .HasDatabaseName(
                "ix_projects_responsible_user_id");

        builder.HasIndex(project => project.CreatedByUserId)
            .HasDatabaseName(
                "ix_projects_created_by_user_id");

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(project => project.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_projects_tenants");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(project => project.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName(
                "fk_projects_created_by_users");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(project => project.ResponsibleUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName(
                "fk_projects_responsible_users");
    }
}