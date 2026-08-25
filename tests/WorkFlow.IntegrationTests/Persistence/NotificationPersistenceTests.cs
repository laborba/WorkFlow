using Microsoft.EntityFrameworkCore;
using WorkFlow.Domain.Entities;
using WorkFlow.Domain.Enums;
using WorkFlow.IntegrationTests.Infrastructure;
using Xunit;

namespace WorkFlow.IntegrationTests.Persistence;

[Collection(TestCollectionNames.PostgreSql)]
public sealed class NotificationPersistenceTests
{
    private readonly PostgreSqlTestDatabase _database;

    public NotificationPersistenceTests(
        PostgreSqlTestDatabase database)
    {
        _database = database;
    }

    [Fact]
    public async Task
        Add_ShouldPersistNotification_WhenDataIsValid()
    {
        await using var context =
            _database.CreateDbContext();

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var uniqueValue =
            Guid.NewGuid().ToString("N");

        var tenant = new Tenant(
            $"Tenant {uniqueValue}",
            $"REG-{uniqueValue}",
            $"tenant-{uniqueValue}@test.local");

        context.Tenants.Add(tenant);

        await context.SaveChangesAsync();

        var recipient = new User(
            tenant.Id,
            "Destinatário",
            $"recipient-{uniqueValue}@test.local",
            "recipient-password-hash",
            UserRole.Member);

        var actor = new User(
            tenant.Id,
            "Autor da Ação",
            $"actor-{uniqueValue}@test.local",
            "actor-password-hash",
            UserRole.ProjectManager);

        context.Users.AddRange(
            recipient,
            actor);

        await context.SaveChangesAsync();

        var notification = new Notification(
            recipient.Id,
            NotificationType.TaskAssigned,
            "Nova tarefa",
            "Uma nova tarefa foi atribuída a você.",
            actor.Id,
            NotificationResourceType.User,
            actor.Id);

        context.Notifications.Add(notification);

        await context.SaveChangesAsync();

        var notificationId = notification.Id;

        context.ChangeTracker.Clear();

        var persistedNotification =
            await context.Notifications.SingleAsync(
                item => item.Id == notificationId);

        Assert.Equal(
            recipient.Id,
            persistedNotification.RecipientUserId);

        Assert.Equal(
            actor.Id,
            persistedNotification.ActorUserId);

        Assert.Equal(
            NotificationType.TaskAssigned,
            persistedNotification.Type);

        Assert.Equal(
            "Nova tarefa",
            persistedNotification.Title);

        Assert.Equal(
            "Uma nova tarefa foi atribuída a você.",
            persistedNotification.Message);

        Assert.Equal(
            NotificationResourceType.User,
            persistedNotification.ResourceType);

        Assert.Equal(
            actor.Id,
            persistedNotification.ResourceId);

        Assert.False(
            persistedNotification.IsRead);

        Assert.False(
            persistedNotification.IsDismissed);

        Assert.Null(
            persistedNotification.ReadAt);

        Assert.Null(
            persistedNotification.DismissedAt);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
    MarkAsReadAndDismiss_ShouldPersistDates()
    {
        await using var context =
            _database.CreateDbContext();

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var uniqueValue =
            Guid.NewGuid().ToString("N");

        var tenant = new Tenant(
            $"Tenant {uniqueValue}",
            $"REG-{uniqueValue}",
            $"tenant-{uniqueValue}@test.local");

        context.Tenants.Add(tenant);

        await context.SaveChangesAsync();

        var recipient = new User(
            tenant.Id,
            "Destinatário",
            $"recipient-{uniqueValue}@test.local",
            "recipient-password-hash",
            UserRole.Member);

        context.Users.Add(recipient);

        await context.SaveChangesAsync();

        var notification = new Notification(
            recipient.Id,
            NotificationType.TaskDueSoon,
            "Prazo próximo",
            "Uma tarefa está próxima do prazo.");

        context.Notifications.Add(notification);

        await context.SaveChangesAsync();

        var notificationId = notification.Id;

        context.ChangeTracker.Clear();

        var notificationToUpdate =
            await context.Notifications.SingleAsync(
                item => item.Id == notificationId);

        var readAt =
            notificationToUpdate.CreatedAt.AddMinutes(1);

        var dismissedAt =
            notificationToUpdate.CreatedAt.AddMinutes(2);

        notificationToUpdate.MarkAsRead(
            recipient.Id,
            readAt);

        notificationToUpdate.Dismiss(
            recipient.Id,
            dismissedAt);

        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var persistedNotification =
            await context.Notifications.SingleAsync(
                item => item.Id == notificationId);

        Assert.True(
            persistedNotification.IsRead);

        Assert.True(
            persistedNotification.IsDismissed);

        Assert.Equal(
            readAt,
            persistedNotification.ReadAt);

        Assert.Equal(
            dismissedAt,
            persistedNotification.DismissedAt);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
    Add_ShouldPersistSystemNotificationWithoutActorOrResource()
    {
        await using var context =
            _database.CreateDbContext();

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var uniqueValue =
            Guid.NewGuid().ToString("N");

        var tenant = new Tenant(
            $"Tenant {uniqueValue}",
            $"REG-{uniqueValue}",
            $"tenant-{uniqueValue}@test.local");

        context.Tenants.Add(tenant);

        await context.SaveChangesAsync();

        var recipient = new User(
            tenant.Id,
            "Destinatário",
            $"recipient-{uniqueValue}@test.local",
            "recipient-password-hash",
            UserRole.Member);

        context.Users.Add(recipient);

        await context.SaveChangesAsync();

        var notification = new Notification(
            recipient.Id,
            NotificationType.TaskOverdue,
            "Tarefa atrasada",
            "Uma tarefa ultrapassou o prazo.");

        context.Notifications.Add(notification);

        await context.SaveChangesAsync();

        var notificationId = notification.Id;

        context.ChangeTracker.Clear();

        var persistedNotification =
            await context.Notifications.SingleAsync(
                item => item.Id == notificationId);

        Assert.Null(
            persistedNotification.ActorUserId);

        Assert.Null(
            persistedNotification.ResourceType);

        Assert.Null(
            persistedNotification.ResourceId);

        Assert.False(
            persistedNotification.IsRetentionExpiredAt(
                persistedNotification.CreatedAt
                    .AddDays(89)));

        Assert.True(
            persistedNotification.IsRetentionExpiredAt(
                persistedNotification.CreatedAt
                    .Add(Notification.RetentionPeriod)));

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
    SaveChanges_ShouldThrow_WhenRecipientDoesNotExist()
    {
        await using var context =
            _database.CreateDbContext();

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var notification = new Notification(
            long.MaxValue,
            NotificationType.TaskAssigned,
            "Nova tarefa",
            "Uma nova tarefa foi atribuída.");

        context.Notifications.Add(notification);

        await Assert.ThrowsAsync<DbUpdateException>(
            () => context.SaveChangesAsync());

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
    SaveChanges_ShouldThrow_WhenActorDoesNotExist()
    {
        await using var context =
            _database.CreateDbContext();

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var uniqueValue =
            Guid.NewGuid().ToString("N");

        var tenant = new Tenant(
            $"Tenant {uniqueValue}",
            $"REG-{uniqueValue}",
            $"tenant-{uniqueValue}@test.local");

        context.Tenants.Add(tenant);

        await context.SaveChangesAsync();

        var recipient = new User(
            tenant.Id,
            "Destinatário",
            $"recipient-{uniqueValue}@test.local",
            "recipient-password-hash",
            UserRole.Member);

        context.Users.Add(recipient);

        await context.SaveChangesAsync();

        var notification = new Notification(
            recipient.Id,
            NotificationType.TaskAssigned,
            "Nova tarefa",
            "Uma nova tarefa foi atribuída.",
            actorUserId: long.MaxValue);

        context.Notifications.Add(notification);

        await Assert.ThrowsAsync<DbUpdateException>(
            () => context.SaveChangesAsync());

        await transaction.RollbackAsync();
    }
}