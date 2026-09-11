using Microsoft.EntityFrameworkCore;
using WorkFlow.Domain.Entities;
using WorkFlow.Domain.Enums;
using WorkFlow.IntegrationTests.Infrastructure;
using Xunit;

namespace WorkFlow.IntegrationTests.Persistence.Tenants;

[Collection(TestCollectionNames.PostgreSql)]
public sealed class TenantPersistenceTests
{
    private readonly PostgreSqlTestDatabase _database;

    public TenantPersistenceTests(
        PostgreSqlTestDatabase database)
    {
        _database = database;
    }

    [Fact]
    public async Task
        Add_ShouldPersistTenant_WhenDataIsValid()
    {
        await using var context =
            _database.CreateDbContext();

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var uniqueValue =
            Guid.NewGuid().ToString("N");

        var registrationNumber =
            $"registration-{uniqueValue}";

        var email =
            $"tenant-{uniqueValue}@test.local";

        var tenant = new Tenant(
            "Empresa de Teste",
            registrationNumber,
            email,
            "(47) 99999-9999");

        context.Tenants.Add(tenant);

        await context.SaveChangesAsync();

        var generatedId = tenant.Id;
        var generatedPublicId = tenant.PublicId;

        context.ChangeTracker.Clear();

        var persistedTenant =
            await context.Tenants.SingleAsync(
                currentTenant =>
                    currentTenant.Id == generatedId);

        Assert.True(persistedTenant.Id > 0);
        Assert.Equal(
            generatedPublicId,
            persistedTenant.PublicId);

        Assert.Equal(
            "Empresa de Teste",
            persistedTenant.Name);

        Assert.Equal(
            registrationNumber,
            persistedTenant.RegistrationNumber);

        Assert.Equal(email, persistedTenant.Email);

        Assert.Equal(
            "(47) 99999-9999",
            persistedTenant.Phone);

        Assert.True(persistedTenant.IsActive);
        Assert.Null(persistedTenant.UpdatedAt);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
    Update_ShouldPersistChanges_WhenTenantIsModified()
    {
        await using var context =
            _database.CreateDbContext();

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var uniqueValue =
            Guid.NewGuid().ToString("N");

        var tenant = new Tenant(
            "Empresa Original",
            $"registration-{uniqueValue}",
            $"original-{uniqueValue}@test.local",
            "(47) 99999-9999");

        context.Tenants.Add(tenant);

        await context.SaveChangesAsync();

        tenant.Rename("Empresa Atualizada");

        tenant.UpdateContact(
            $"updated-{uniqueValue}@test.local");

        tenant.Deactivate();

        await context.SaveChangesAsync();

        var tenantId = tenant.Id;

        context.ChangeTracker.Clear();

        var persistedTenant =
            await context.Tenants.SingleAsync(
                currentTenant =>
                    currentTenant.Id == tenantId);

        Assert.Equal(
            "Empresa Atualizada",
            persistedTenant.Name);

        Assert.Equal(
            $"updated-{uniqueValue}@test.local",
            persistedTenant.Email);

        Assert.Null(persistedTenant.Phone);
        Assert.False(persistedTenant.IsActive);
        Assert.NotNull(persistedTenant.UpdatedAt);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
    SaveChanges_ShouldThrow_WhenRegistrationNumberIsDuplicated()
    {
        await using var context =
            _database.CreateDbContext();

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var uniqueValue =
            Guid.NewGuid().ToString("N");

        var registrationNumber =
            $"registration-{uniqueValue}";

        var firstTenant = new Tenant(
            "Primeira Empresa",
            registrationNumber,
            $"first-{uniqueValue}@test.local");

        var secondTenant = new Tenant(
            "Segunda Empresa",
            registrationNumber,
            $"second-{uniqueValue}@test.local");

        context.Tenants.Add(firstTenant);

        await context.SaveChangesAsync();

        context.Tenants.Add(secondTenant);

        await Assert.ThrowsAsync<DbUpdateException>(
            () => context.SaveChangesAsync());

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
    SaveChanges_ShouldThrow_WhenEmailIsDuplicatedInSameTenant()
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

        var email =
            $"usuario-{uniqueValue}@test.local";

        var firstUser = new User(
            tenant.Id,
            "Primeiro Usuário",
            email,
            "first-password-hash",
            UserRole.Member);

        var secondUser = new User(
            tenant.Id,
            "Segundo Usuário",
            email,
            "second-password-hash",
            UserRole.Member);

        context.Users.Add(firstUser);

        await context.SaveChangesAsync();

        context.Users.Add(secondUser);

        await Assert.ThrowsAsync<DbUpdateException>(
            () => context.SaveChangesAsync());

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
    Add_ShouldAllowSameEmailInDifferentTenants()
    {
        await using var context =
            _database.CreateDbContext();

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var uniqueValue =
            Guid.NewGuid().ToString("N");

        var firstTenant = new Tenant(
            $"Primeiro Tenant {uniqueValue}",
            $"REG-A-{uniqueValue}",
            $"tenant-a-{uniqueValue}@test.local");

        var secondTenant = new Tenant(
            $"Segundo Tenant {uniqueValue}",
            $"REG-B-{uniqueValue}",
            $"tenant-b-{uniqueValue}@test.local");

        context.Tenants.AddRange(
            firstTenant,
            secondTenant);

        await context.SaveChangesAsync();

        var sharedEmail =
            $"usuario-{uniqueValue}@test.local";

        var firstUser = new User(
            firstTenant.Id,
            "Usuário do Primeiro Tenant",
            sharedEmail,
            "first-password-hash",
            UserRole.Member);

        var secondUser = new User(
            secondTenant.Id,
            "Usuário do Segundo Tenant",
            sharedEmail,
            "second-password-hash",
            UserRole.Member);

        context.Users.AddRange(
            firstUser,
            secondUser);

        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var persistedUsers =
            await context.Users
                .Where(user => user.Email == sharedEmail)
                .ToListAsync();

        Assert.Equal(2, persistedUsers.Count);

        Assert.Contains(
            persistedUsers,
            user => user.TenantId == firstTenant.Id);

        Assert.Contains(
            persistedUsers,
            user => user.TenantId == secondTenant.Id);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
    Update_ShouldPersistChanges_WhenUserIsModified()
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

        var user = new User(
            tenant.Id,
            "Nome Original",
            $"original-{uniqueValue}@test.local",
            "original-password-hash",
            UserRole.Member);

        context.Users.Add(user);

        await context.SaveChangesAsync();

        var publicId = user.PublicId;

        user.Rename("Nome Atualizado");
        user.ChangeEmail(
            $"atualizado-{uniqueValue}@test.local");
        user.ChangePasswordHash(
            "updated-password-hash");
        user.ChangeRole(
            UserRole.ProjectManager);
        user.Deactivate();

        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var persistedUser =
            await context.Users.SingleAsync(
                item => item.PublicId == publicId);

        Assert.Equal(
            "Nome Atualizado",
            persistedUser.Name);

        Assert.Equal(
            $"atualizado-{uniqueValue}@test.local",
            persistedUser.Email);

        Assert.Equal(
            "updated-password-hash",
            persistedUser.PasswordHash);

        Assert.Equal(
            UserRole.ProjectManager,
            persistedUser.Role);

        Assert.False(persistedUser.IsActive);
        Assert.NotNull(persistedUser.UpdatedAt);

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

        var user = new User(
            long.MaxValue,
            "Usuário sem Tenant",
            $"usuario-{uniqueValue}@test.local",
            "password-hash",
            UserRole.Member);

        context.Users.Add(user);

        await Assert.ThrowsAsync<DbUpdateException>(
            () => context.SaveChangesAsync());

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
    SaveChanges_ShouldThrow_WhenDeletingTenantWithUser()
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

        var user = new User(
            tenant.Id,
            "Usuário Vinculado",
            $"usuario-{uniqueValue}@test.local",
            "password-hash",
            UserRole.Member);

        context.Users.Add(user);

        await context.SaveChangesAsync();

        context.Tenants.Remove(tenant);

        await Assert.ThrowsAsync<DbUpdateException>(
            () => context.SaveChangesAsync());

        await transaction.RollbackAsync();
    }
}