using Microsoft.EntityFrameworkCore;
using WorkFlow.Domain.Entities;
using WorkFlow.Domain.Enums;
using WorkFlow.IntegrationTests.Infrastructure;
using Xunit;
using WorkFlow.Infrastructure.Persistence.Repositories;


namespace WorkFlow.IntegrationTests.Persistence;

[Collection(TestCollectionNames.PostgreSql)]
public sealed class UserPersistenceTests
{
    private readonly PostgreSqlTestDatabase _database;

    public UserPersistenceTests(
        PostgreSqlTestDatabase database)
    {
        _database = database;
    }

    [Fact]
    public async Task
        Add_ShouldPersistTenantUser_WhenDataIsValid()
    {
        await using var context =
            _database.CreateDbContext();

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var uniqueValue =
            Guid.NewGuid().ToString("N");

        var tenant = new Tenant(
            "Empresa do Usuário",
            $"registration-{uniqueValue}",
            $"tenant-{uniqueValue}@test.local");

        context.Tenants.Add(tenant);

        await context.SaveChangesAsync();

        var user = new User(
            tenant.Id,
            "Usuário de Teste",
            $"user-{uniqueValue}@test.local",
            "password-hash-for-test",
            UserRole.TenantAdmin);

        context.Users.Add(user);

        await context.SaveChangesAsync();

        var generatedId = user.Id;
        var generatedPublicId = user.PublicId;

        context.ChangeTracker.Clear();

        var persistedUser =
            await context.Users.SingleAsync(
                currentUser =>
                    currentUser.Id == generatedId);

        Assert.True(persistedUser.Id > 0);

        Assert.Equal(
            generatedPublicId,
            persistedUser.PublicId);

        Assert.Equal(
            tenant.Id,
            persistedUser.TenantId);

        Assert.Equal(
            "Usuário de Teste",
            persistedUser.Name);

        Assert.Equal(
            $"user-{uniqueValue}@test.local",
            persistedUser.Email);

        Assert.Equal(
            "password-hash-for-test",
            persistedUser.PasswordHash);

        Assert.Equal(
            UserRole.TenantAdmin,
            persistedUser.Role);

        Assert.True(persistedUser.IsActive);
        Assert.Null(persistedUser.UpdatedAt);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
    Add_ShouldPersistSystemAdminWithoutTenant_WhenDataIsValid()
    {
        await using var context =
            _database.CreateDbContext();

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var uniqueValue =
            Guid.NewGuid().ToString("N");

        var systemAdmin = new User(
            null,
            "Administrador do Sistema",
            $"admin-{uniqueValue}@test.local",
            "system-admin-password-hash",
            UserRole.SystemAdmin);

        context.Users.Add(systemAdmin);

        await context.SaveChangesAsync();

        var generatedId = systemAdmin.Id;

        context.ChangeTracker.Clear();

        var persistedUser =
            await context.Users.SingleAsync(
                currentUser =>
                    currentUser.Id == generatedId);

        Assert.True(persistedUser.Id > 0);
        Assert.Null(persistedUser.TenantId);

        Assert.Equal(
            UserRole.SystemAdmin,
            persistedUser.Role);

        Assert.Equal(
            "Administrador do Sistema",
            persistedUser.Name);

        Assert.Equal(
            $"admin-{uniqueValue}@test.local",
            persistedUser.Email);

        Assert.True(persistedUser.IsActive);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
    SaveChanges_ShouldThrow_WhenSystemAdminEmailIsDuplicated()
    {
        await using var context =
            _database.CreateDbContext();

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var uniqueValue =
            Guid.NewGuid().ToString("N");

        var email =
            $"admin-{uniqueValue}@test.local";

        var firstSystemAdmin = new User(
            null,
            "Primeiro Administrador",
            email,
            "first-password-hash",
            UserRole.SystemAdmin);

        var secondSystemAdmin = new User(
            null,
            "Segundo Administrador",
            email,
            "second-password-hash",
            UserRole.SystemAdmin);

        context.Users.Add(firstSystemAdmin);

        await context.SaveChangesAsync();

        context.Users.Add(secondSystemAdmin);

        await Assert.ThrowsAsync<DbUpdateException>(
            () => context.SaveChangesAsync());

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
ExistsByEmailAsync_ShouldReturnTrue_WhenEmailDiffersOnlyByCase()
    {
        await using var context =
            _database.CreateDbContext();

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var uniqueValue =
            Guid.NewGuid().ToString("N");

        var tenant = new Tenant(
            "Empresa do Usuário",
            $"registration-{uniqueValue}",
            $"tenant-{uniqueValue}@test.local");

        context.Tenants.Add(tenant);

        await context.SaveChangesAsync();

        var email =
            $"usuario-{uniqueValue}@test.local";

        var user = new User(
            tenant.Id,
            "Usuário de Teste",
            email,
            "password-hash-for-test",
            UserRole.Member);

        context.Users.Add(user);

        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var repository =
            new UserRepository(context);

        var emailWithDifferentCase =
            email.ToUpperInvariant();

        var exists =
            await repository.ExistsByEmailAsync(
                tenant.Id,
                emailWithDifferentCase);

        Assert.True(exists);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
SaveChanges_ShouldThrow_WhenTenantUserEmailDiffersOnlyByCase()
    {
        await using var context =
            _database.CreateDbContext();

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var uniqueValue =
            Guid.NewGuid().ToString("N");

        var tenant = new Tenant(
            "Empresa do Usuário",
            $"registration-{uniqueValue}",
            $"tenant-{uniqueValue}@test.local");

        context.Tenants.Add(tenant);

        await context.SaveChangesAsync();

        var email =
            $"usuario-{uniqueValue}@test.local";

        var firstUser = new User(
            tenant.Id,
            "Primeiro Usuário",
            email,
            "first-password-hash",
            UserRole.Member);

        context.Users.Add(firstUser);

        await context.SaveChangesAsync();

        var secondUser = new User(
            tenant.Id,
            "Segundo Usuário",
            email.ToUpperInvariant(),
            "second-password-hash",
            UserRole.Member);

        context.Users.Add(secondUser);

        await Assert.ThrowsAsync<DbUpdateException>(
            () => context.SaveChangesAsync());

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
SaveChanges_ShouldAllowSameEmail_WhenUsersBelongToDifferentTenants()
    {
        await using var context =
            _database.CreateDbContext();

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var uniqueValue =
            Guid.NewGuid().ToString("N");

        var firstTenant = new Tenant(
            "Primeira Empresa",
            $"registration-first-{uniqueValue}",
            $"tenant-first-{uniqueValue}@test.local");

        var secondTenant = new Tenant(
            "Segunda Empresa",
            $"registration-second-{uniqueValue}",
            $"tenant-second-{uniqueValue}@test.local");

        context.Tenants.Add(firstTenant);
        context.Tenants.Add(secondTenant);

        await context.SaveChangesAsync();

        var email =
            $"usuario-{uniqueValue}@test.local";

        var firstUser = new User(
            firstTenant.Id,
            "Primeiro Usuário",
            email,
            "first-password-hash",
            UserRole.Member);

        var secondUser = new User(
            secondTenant.Id,
            "Segundo Usuário",
            email.ToUpperInvariant(),
            "second-password-hash",
            UserRole.Member);

        context.Users.Add(firstUser);
        context.Users.Add(secondUser);

        await context.SaveChangesAsync();

        Assert.True(firstUser.Id > 0);
        Assert.True(secondUser.Id > 0);

        Assert.NotEqual(
            firstUser.TenantId,
            secondUser.TenantId);

        Assert.Equal(
            firstUser.NormalizedEmail,
            secondUser.NormalizedEmail);

        await transaction.RollbackAsync();
    }
}