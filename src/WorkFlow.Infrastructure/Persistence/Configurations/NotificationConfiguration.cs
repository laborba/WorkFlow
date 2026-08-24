using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkFlow.Domain.Entities;

namespace WorkFlow.Infrastructure.Persistence.Configurations;

public sealed class NotificationConfiguration
    : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("notifications");

        builder.HasKey(notification => notification.Id)
            .HasName("pk_notifications");

        builder.Property(notification => notification.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(notification => notification.RecipientUserId)
            .HasColumnName("recipient_user_id")
            .IsRequired();

        builder.Property(notification => notification.ActorUserId)
            .HasColumnName("actor_user_id");

        builder.Property(notification => notification.Type)
            .HasColumnName("type")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(notification => notification.Title)
            .HasColumnName("title")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(notification => notification.Message)
            .HasColumnName("message")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(notification => notification.ResourceType)
            .HasColumnName("resource_type")
            .HasConversion<int>();

        builder.Property(notification => notification.ResourceId)
            .HasColumnName("resource_id");

        builder.Property(notification => notification.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(notification => notification.ReadAt)
            .HasColumnName("read_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(notification => notification.DismissedAt)
            .HasColumnName("dismissed_at")
            .HasColumnType("timestamp with time zone");

        builder.Ignore(notification => notification.IsRead);
        builder.Ignore(notification => notification.IsDismissed);

        builder.HasIndex(notification => new
        {
            notification.RecipientUserId,
            notification.CreatedAt
        })
            .HasDatabaseName(
                "ix_notifications_recipient_user_id_created_at");

        builder.HasIndex(notification => notification.ActorUserId)
            .HasDatabaseName("ix_notifications_actor_user_id");

        builder.HasIndex(notification => new
        {
            notification.ResourceType,
            notification.ResourceId
        })
            .HasFilter(
                "resource_type IS NOT NULL AND resource_id IS NOT NULL")
            .HasDatabaseName(
                "ix_notifications_resource_type_resource_id");

        builder.HasIndex(notification => notification.CreatedAt)
            .HasDatabaseName("ix_notifications_created_at");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(notification =>
                notification.RecipientUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName(
                "fk_notifications_users_recipient_user_id");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(notification =>
                notification.ActorUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName(
                "fk_notifications_users_actor_user_id");
    }
}