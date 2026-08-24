using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkFlow.Domain.Entities;

namespace WorkFlow.Infrastructure.Persistence.Configurations;

public sealed class ChatMessageConfiguration
    : IEntityTypeConfiguration<ChatMessage>
{
    public void Configure(EntityTypeBuilder<ChatMessage> builder)
    {
        builder.ToTable("chat_messages");

        builder.HasKey(chatMessage => chatMessage.Id)
            .HasName("pk_chat_messages");

        builder.Property(chatMessage => chatMessage.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(chatMessage => chatMessage.TenantId)
            .HasColumnName("tenant_id")
            .IsRequired();

        builder.Property(chatMessage => chatMessage.AuthorUserId)
            .HasColumnName("author_user_id")
            .IsRequired();

        builder.HasIndex(chatMessage =>
            chatMessage.DeletedByUserId)
        .HasDatabaseName(
            "ix_chat_messages_deleted_by_user_id");

        builder.Property(chatMessage => chatMessage.Content)
            .HasColumnName("content")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(chatMessage => chatMessage.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(chatMessage => chatMessage.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(chatMessage => chatMessage.ExpiresAt)
            .HasColumnName("expires_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(chatMessage => chatMessage.DeletedAt)
            .HasColumnName("deleted_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(chatMessage => chatMessage.DeletedByUserId)
            .HasColumnName("deleted_by_user_id");

        builder.Ignore(chatMessage => chatMessage.IsDeleted);

        builder.HasIndex(chatMessage => new
        {
            chatMessage.TenantId,
            chatMessage.CreatedAt
        })
            .HasDatabaseName(
                "ix_chat_messages_tenant_id_created_at");

        builder.HasIndex(chatMessage => chatMessage.AuthorUserId)
            .HasDatabaseName("ix_chat_messages_author_user_id");

        builder.HasIndex(chatMessage => chatMessage.ExpiresAt)
            .HasDatabaseName("ix_chat_messages_expires_at");

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(chatMessage => chatMessage.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName(
                "fk_chat_messages_tenants_tenant_id");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(chatMessage =>
                chatMessage.AuthorUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName(
                "fk_chat_messages_users_author_user_id");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(chatMessage =>
                chatMessage.DeletedByUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName(
                "fk_chat_messages_users_deleted_by_user_id");
    }
}