using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkFlow.Domain.Entities;

namespace WorkFlow.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration :
    IEntityTypeConfiguration<User>
{
    public void Configure(
        EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(user => user.Id)
            .HasName("pk_users");

        builder.Property(user => user.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(user => user.PublicId)
            .HasColumnName("public_id")
            .ValueGeneratedNever()
            .IsRequired();

        builder.Property(user => user.TenantId)
            .HasColumnName("tenant_id");

        builder.Property(user => user.Name)
            .HasColumnName("name")
            .IsRequired();

        builder.Property(user => user.Email)
            .HasColumnName("email")
            .IsRequired();

        builder.Property(user => user.PasswordHash)
            .HasColumnName("password_hash")
            .IsRequired();

        builder.Property(user => user.Role)
            .HasColumnName("role")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(user => user.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        builder.Property(user => user.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(user => user.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone");

        builder.HasIndex(user => user.PublicId)
            .IsUnique()
            .HasDatabaseName("ux_users_public_id");

        builder.HasIndex(
                user => new
                {
                    user.TenantId,
                    user.Email
                })
            .IsUnique()
            .HasFilter("tenant_id IS NOT NULL")
            .HasDatabaseName(
                "ux_users_tenant_id_email");

        builder.HasIndex(user => user.Email)
            .IsUnique()
            .HasFilter("tenant_id IS NULL")
            .HasDatabaseName(
                "ux_users_system_admin_email");

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(user => user.TenantId)
            .OnDelete(DeleteBehavior.ClientNoAction)
            .HasConstraintName("fk_users_tenants");
    }
}