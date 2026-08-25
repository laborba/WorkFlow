using Microsoft.EntityFrameworkCore;
using WorkFlow.Domain.Entities;
using WorkFlow.Domain.Enums;
using WorkFlow.IntegrationTests.Infrastructure;
using Xunit;

namespace WorkFlow.IntegrationTests.Persistence;

[Collection(TestCollectionNames.PostgreSql)]
public sealed class ChatMessagePersistenceTests
{
    private readonly PostgreSqlTestDatabase _database;

    public ChatMessagePersistenceTests(
        PostgreSqlTestDatabase database)
    {
        _database = database;
    }

    [Fact]
    public async Task
        Add_ShouldPersistChatMessage_WhenDataIsValid()
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

        var author = new User(
            tenant.Id,
            "Autor da Mensagem",
            $"author-{uniqueValue}@test.local",
            "author-password-hash",
            UserRole.Member);

        context.Users.Add(author);

        await context.SaveChangesAsync();

        var message = new ChatMessage(
            tenant.Id,
            author.Id,
            "Mensagem do chat");

        context.ChatMessages.Add(message);

        await context.SaveChangesAsync();

        var messageId = message.Id;

        context.ChangeTracker.Clear();

        var persistedMessage =
            await context.ChatMessages.SingleAsync(
                item => item.Id == messageId);

        Assert.Equal(
            tenant.Id,
            persistedMessage.TenantId);

        Assert.Equal(
            author.Id,
            persistedMessage.AuthorUserId);

        Assert.Equal(
            "Mensagem do chat",
            persistedMessage.Content);

        Assert.Equal(
            persistedMessage.CreatedAt.Add(
                ChatMessage.RetentionPeriod),
            persistedMessage.ExpiresAt);

        Assert.False(
            persistedMessage.IsDeleted);

        Assert.Null(
            persistedMessage.UpdatedAt);

        Assert.Null(
            persistedMessage.DeletedAt);

        Assert.Null(
            persistedMessage.DeletedByUserId);

        Assert.NotEqual(
            default,
            persistedMessage.CreatedAt);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
    Edit_ShouldPersistContentAndUpdatedAt_WhenWithinEditWindow()
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

        var author = new User(
            tenant.Id,
            "Autor da Mensagem",
            $"author-{uniqueValue}@test.local",
            "author-password-hash",
            UserRole.Member);

        context.Users.Add(author);

        await context.SaveChangesAsync();

        var message = new ChatMessage(
            tenant.Id,
            author.Id,
            "Mensagem original");

        context.ChatMessages.Add(message);

        await context.SaveChangesAsync();

        var messageId = message.Id;

        context.ChangeTracker.Clear();

        var messageToEdit =
            await context.ChatMessages.SingleAsync(
                item => item.Id == messageId);

        var editedAt =
            messageToEdit.CreatedAt.AddMinutes(2);

        messageToEdit.Edit(
            "Mensagem atualizada",
            author.Id,
            editedAt);

        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var persistedMessage =
            await context.ChatMessages.SingleAsync(
                item => item.Id == messageId);

        Assert.Equal(
            "Mensagem atualizada",
            persistedMessage.Content);

        Assert.Equal(
            editedAt,
            persistedMessage.UpdatedAt);

        Assert.False(
            persistedMessage.IsDeleted);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
    Delete_ShouldPersistDeletion_WhenAuthorIsWithinEditWindow()
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

        var author = new User(
            tenant.Id,
            "Autor da Mensagem",
            $"author-{uniqueValue}@test.local",
            "author-password-hash",
            UserRole.Member);

        context.Users.Add(author);

        await context.SaveChangesAsync();

        var message = new ChatMessage(
            tenant.Id,
            author.Id,
            "Mensagem que será removida");

        context.ChatMessages.Add(message);

        await context.SaveChangesAsync();

        var messageId = message.Id;

        context.ChangeTracker.Clear();

        var messageToDelete =
            await context.ChatMessages.SingleAsync(
                item => item.Id == messageId);

        var deletedAt =
            messageToDelete.CreatedAt.AddMinutes(3);

        messageToDelete.Delete(
            author.Id,
            deletedAt);

        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var persistedMessage =
            await context.ChatMessages.SingleAsync(
                item => item.Id == messageId);

        Assert.True(
            persistedMessage.IsDeleted);

        Assert.Equal(
            deletedAt,
            persistedMessage.DeletedAt);

        Assert.Equal(
            author.Id,
            persistedMessage.DeletedByUserId);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
    DeleteByModerator_ShouldPersistDeletion_AfterExpiration()
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

        var author = new User(
            tenant.Id,
            "Autor da Mensagem",
            $"author-{uniqueValue}@test.local",
            "author-password-hash",
            UserRole.Member);

        var moderator = new User(
            tenant.Id,
            "Moderador do Chat",
            $"moderator-{uniqueValue}@test.local",
            "moderator-password-hash",
            UserRole.ProjectManager);

        context.Users.AddRange(
            author,
            moderator);

        await context.SaveChangesAsync();

        var message = new ChatMessage(
            tenant.Id,
            author.Id,
            "Mensagem expirada");

        context.ChatMessages.Add(message);

        await context.SaveChangesAsync();

        var messageId = message.Id;

        context.ChangeTracker.Clear();

        var messageToDelete =
            await context.ChatMessages.SingleAsync(
                item => item.Id == messageId);

        var deletedAt =
            messageToDelete.ExpiresAt.AddMinutes(1);

        Assert.True(
            messageToDelete.IsExpiredAt(deletedAt));

        messageToDelete.DeleteByModerator(
            moderator.Id,
            deletedAt);

        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var persistedMessage =
            await context.ChatMessages.SingleAsync(
                item => item.Id == messageId);

        Assert.True(
            persistedMessage.IsDeleted);

        Assert.Equal(
            deletedAt,
            persistedMessage.DeletedAt);

        Assert.Equal(
            moderator.Id,
            persistedMessage.DeletedByUserId);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
    SaveChanges_ShouldThrow_WhenTenantDoesNotExist()
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

        var author = new User(
            tenant.Id,
            "Autor da Mensagem",
            $"author-{uniqueValue}@test.local",
            "author-password-hash",
            UserRole.Member);

        context.Users.Add(author);

        await context.SaveChangesAsync();

        var message = new ChatMessage(
            long.MaxValue,
            author.Id,
            "Mensagem sem Tenant existente");

        context.ChatMessages.Add(message);

        await Assert.ThrowsAsync<DbUpdateException>(
            () => context.SaveChangesAsync());

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
    SaveChanges_ShouldThrow_WhenAuthorDoesNotExist()
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

        var message = new ChatMessage(
            tenant.Id,
            long.MaxValue,
            "Mensagem sem autor existente");

        context.ChatMessages.Add(message);

        await Assert.ThrowsAsync<DbUpdateException>(
            () => context.SaveChangesAsync());

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
    SaveChanges_ShouldThrow_WhenDeletedByUserDoesNotExist()
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

        var author = new User(
            tenant.Id,
            "Autor da Mensagem",
            $"author-{uniqueValue}@test.local",
            "author-password-hash",
            UserRole.Member);

        context.Users.Add(author);

        await context.SaveChangesAsync();

        var message = new ChatMessage(
            tenant.Id,
            author.Id,
            "Mensagem que será removida");

        context.ChatMessages.Add(message);

        await context.SaveChangesAsync();

        var deletedAt =
            message.CreatedAt.AddMinutes(1);

        message.DeleteByModerator(
            long.MaxValue,
            deletedAt);

        await Assert.ThrowsAsync<DbUpdateException>(
            () => context.SaveChangesAsync());

        await transaction.RollbackAsync();
    }
}