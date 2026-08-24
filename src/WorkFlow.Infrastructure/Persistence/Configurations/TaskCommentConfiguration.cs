using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkFlow.Domain.Entities;

namespace WorkFlow.Infrastructure.Persistence.Configurations;

public sealed class TaskCommentConfiguration
    : IEntityTypeConfiguration<TaskComment>
{
    public void Configure(EntityTypeBuilder<TaskComment> builder)
    {
        builder.ToTable("task_comments");

        builder.HasKey(taskComment => taskComment.Id)
            .HasName("pk_task_comments");

        builder.Property(taskComment => taskComment.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(taskComment => taskComment.TaskId)
            .HasColumnName("task_id")
            .IsRequired();

        builder.Property(taskComment => taskComment.AuthorUserId)
            .HasColumnName("author_user_id")
            .IsRequired();

        builder.HasIndex(taskComment =>
            taskComment.DeletedByUserId)
        .HasDatabaseName(
            "ix_task_comments_deleted_by_user_id");

        builder.Property(taskComment => taskComment.Content)
            .HasColumnName("content")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(taskComment => taskComment.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(taskComment => taskComment.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(taskComment => taskComment.DeletedAt)
            .HasColumnName("deleted_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(taskComment => taskComment.DeletedByUserId)
            .HasColumnName("deleted_by_user_id");

        builder.Ignore(taskComment => taskComment.IsDeleted);

        builder.HasIndex(taskComment => new
        {
            taskComment.TaskId,
            taskComment.CreatedAt
        })
            .HasDatabaseName(
                "ix_task_comments_task_id_created_at");

        builder.HasIndex(taskComment => taskComment.AuthorUserId)
            .HasDatabaseName("ix_task_comments_author_user_id");

        builder.HasOne<ProjectTask>()
            .WithMany()
            .HasForeignKey(taskComment => taskComment.TaskId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName(
                "fk_task_comments_project_tasks_task_id");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(taskComment => taskComment.AuthorUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName(
                "fk_task_comments_users_author_user_id");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(taskComment =>
                taskComment.DeletedByUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName(
                "fk_task_comments_users_deleted_by_user_id");
    }
}