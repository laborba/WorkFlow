using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkFlow.Domain.Entities;

namespace WorkFlow.Infrastructure.Persistence.Configurations;

public sealed class TenantConfiguration :
    IEntityTypeConfiguration<Tenant>
{
    public void Configure(
        EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("tenants");

        builder.HasKey(tenant => tenant.Id)
            .HasName("pk_tenants");

        builder.Property(tenant => tenant.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(tenant => tenant.PublicId)
            .HasColumnName("public_id")
            .ValueGeneratedNever()
            .IsRequired();

        builder.Property(tenant => tenant.Name)
            .HasColumnName("name")
            .IsRequired();

        builder.Property(tenant => tenant.RegistrationNumber)
            .HasColumnName("registration_number")
            .IsRequired();

        builder.Property(tenant => tenant.Email)
            .HasColumnName("email")
            .IsRequired();

        builder.Property(tenant => tenant.Phone)
            .HasColumnName("phone");

        builder.Property(tenant => tenant.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        builder.Property(tenant => tenant.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(tenant => tenant.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone");

        builder.HasIndex(tenant => tenant.PublicId)
            .IsUnique()
            .HasDatabaseName("ux_tenants_public_id");

        builder.HasIndex(tenant => tenant.RegistrationNumber)
            .IsUnique()
            .HasDatabaseName(
                "ux_tenants_registration_number");
    }
}