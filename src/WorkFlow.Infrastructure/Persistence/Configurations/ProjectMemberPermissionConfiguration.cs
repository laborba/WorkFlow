using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkFlow.Domain.Entities;

namespace WorkFlow.Infrastructure.Persistence.Configurations;

public sealed class ProjectMemberPermissionConfiguration :
    IEntityTypeConfiguration<ProjectMemberPermission>
{
    public void Configure(
        EntityTypeBuilder<ProjectMemberPermission> builder)
    {
        builder.ToTable("project_member_permissions");

        builder.HasKey(permission => permission.Id)
            .HasName("pk_project_member_permissions");

        builder.Property(permission => permission.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(permission =>
                permission.ProjectMemberId)
            .HasColumnName("project_member_id")
            .IsRequired();

        builder.Property(permission => permission.Permission)
            .HasColumnName("permission")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(permission =>
                permission.GrantedByUserId)
            .HasColumnName("granted_by_user_id")
            .IsRequired();

        builder.Property(permission => permission.GrantedAt)
            .HasColumnName("granted_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(permission =>
                permission.RevokedByUserId)
            .HasColumnName("revoked_by_user_id");

        builder.Property(permission => permission.RevokedAt)
            .HasColumnName("revoked_at")
            .HasColumnType("timestamp with time zone");

        builder.HasIndex(
                permission => new
                {
                    permission.ProjectMemberId,
                    permission.Permission
                })
            .IsUnique()
            .HasFilter("revoked_at IS NULL")
            .HasDatabaseName(
                "ux_project_member_permissions_active");

        builder.HasIndex(permission =>
                permission.GrantedByUserId)
            .HasDatabaseName(
                "ix_project_member_permissions_granted_by");

        builder.HasIndex(permission =>
                permission.RevokedByUserId)
            .HasDatabaseName(
                "ix_project_member_permissions_revoked_by");

        builder.HasOne<ProjectMember>()
            .WithMany()
            .HasForeignKey(permission =>
                permission.ProjectMemberId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName(
                "fk_project_member_permissions_members");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(permission =>
                permission.GrantedByUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName(
                "fk_project_member_permissions_granted_by_users");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(permission =>
                permission.RevokedByUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName(
                "fk_project_member_permissions_revoked_by_users");
    }
}