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

    [Fact]
    public async Task
    GetByPublicIdAsync_ShouldReturnNull_WhenUserBelongsToDifferentTenant()
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
            $"first-{uniqueValue}@test.local");

        var secondTenant = new Tenant(
            "Segunda Empresa",
            $"registration-second-{uniqueValue}",
            $"second-{uniqueValue}@test.local");

        context.Tenants.Add(firstTenant);
        context.Tenants.Add(secondTenant);

        await context.SaveChangesAsync();

        var user = new User(
            secondTenant.Id,
            "Usuário da Segunda Empresa",
            $"user-{uniqueValue}@test.local",
            "password-hash-for-test",
            UserRole.Member);

        context.Users.Add(user);

        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var repository =
            new UserRepository(context);

        var result =
            await repository.GetByPublicIdAsync(
                firstTenant.Id,
                user.PublicId);

        Assert.Null(result);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
    GetPagedAsync_ShouldReturnOnlyUsersFromRequestedTenant()
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
            $"first-{uniqueValue}@test.local");

        var secondTenant = new Tenant(
            "Segunda Empresa",
            $"registration-second-{uniqueValue}",
            $"second-{uniqueValue}@test.local");

        context.Tenants.Add(firstTenant);
        context.Tenants.Add(secondTenant);

        await context.SaveChangesAsync();

        var firstTenantUser = new User(
            firstTenant.Id,
            "Usuário Primeira Empresa",
            $"first-user-{uniqueValue}@test.local",
            "password-hash-for-test",
            UserRole.Member);

        var secondTenantUser = new User(
            secondTenant.Id,
            "Usuário Segunda Empresa",
            $"second-user-{uniqueValue}@test.local",
            "password-hash-for-test",
            UserRole.Member);

        context.Users.Add(firstTenantUser);
        context.Users.Add(secondTenantUser);

        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var repository =
            new UserRepository(context);

        var result =
            await repository.GetPagedAsync(
                firstTenant.Id,
                pageNumber: 1,
                pageSize: 20,
                role: null,
                isActive: null,
                search: null);

        var user =
            Assert.Single(result.Items);

        Assert.Equal(
            firstTenantUser.PublicId,
            user.PublicId);

        Assert.Equal(
            firstTenant.Id,
            user.TenantId);

        Assert.Equal(
            1,
            result.TotalCount);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
    GetPagedAsync_ShouldFilterUsersByRole()
    {
        await using var context =
            _database.CreateDbContext();

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var uniqueValue =
            Guid.NewGuid().ToString("N");

        var tenant = new Tenant(
            "Empresa dos Usuários",
            $"registration-{uniqueValue}",
            $"tenant-{uniqueValue}@test.local");

        context.Tenants.Add(tenant);

        await context.SaveChangesAsync();

        var member = new User(
            tenant.Id,
            "Usuário Member",
            $"member-{uniqueValue}@test.local",
            "password-hash-for-test",
            UserRole.Member);

        var projectManager = new User(
            tenant.Id,
            "Usuário Gerente",
            $"manager-{uniqueValue}@test.local",
            "password-hash-for-test",
            UserRole.ProjectManager);

        context.Users.Add(member);
        context.Users.Add(projectManager);

        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var repository =
            new UserRepository(context);

        var result =
            await repository.GetPagedAsync(
                tenant.Id,
                pageNumber: 1,
                pageSize: 20,
                role: UserRole.ProjectManager,
                isActive: null,
                search: null);

        var user =
            Assert.Single(result.Items);

        Assert.Equal(
            projectManager.PublicId,
            user.PublicId);

        Assert.Equal(
            UserRole.ProjectManager,
            user.Role);

        Assert.Equal(
            1,
            result.TotalCount);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
    GetPagedAsync_ShouldFilterUsersByIsActive()
    {
        await using var context =
            _database.CreateDbContext();

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var uniqueValue =
            Guid.NewGuid().ToString("N");

        var tenant = new Tenant(
            "Empresa dos Usuários",
            $"registration-{uniqueValue}",
            $"tenant-{uniqueValue}@test.local");

        context.Tenants.Add(tenant);

        await context.SaveChangesAsync();

        var activeUser = new User(
            tenant.Id,
            "Usuário Ativo",
            $"active-{uniqueValue}@test.local",
            "password-hash-for-test",
            UserRole.Member);

        var inactiveUser = new User(
            tenant.Id,
            "Usuário Inativo",
            $"inactive-{uniqueValue}@test.local",
            "password-hash-for-test",
            UserRole.Member);

        inactiveUser.Deactivate();

        context.Users.Add(activeUser);
        context.Users.Add(inactiveUser);

        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var repository =
            new UserRepository(context);

        var result =
            await repository.GetPagedAsync(
                tenant.Id,
                pageNumber: 1,
                pageSize: 20,
                role: null,
                isActive: false,
                search: null);

        var user =
            Assert.Single(result.Items);

        Assert.Equal(
            inactiveUser.PublicId,
            user.PublicId);

        Assert.False(
            user.IsActive);

        Assert.Equal(
            1,
            result.TotalCount);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
    GetPagedAsync_ShouldFilterUsersByNameSearch()
    {
        await using var context =
            _database.CreateDbContext();

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var uniqueValue =
            Guid.NewGuid().ToString("N");

        var tenant = new Tenant(
            "Empresa dos Usuários",
            $"registration-{uniqueValue}",
            $"tenant-{uniqueValue}@test.local");

        context.Tenants.Add(tenant);

        await context.SaveChangesAsync();

        var firstUser = new User(
            tenant.Id,
            "Lucas Aaron",
            $"lucas-{uniqueValue}@test.local",
            "password-hash-for-test",
            UserRole.Member);

        var secondUser = new User(
            tenant.Id,
            "Maria Silva",
            $"maria-{uniqueValue}@test.local",
            "password-hash-for-test",
            UserRole.Member);

        context.Users.Add(firstUser);
        context.Users.Add(secondUser);

        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var repository =
            new UserRepository(context);

        var result =
            await repository.GetPagedAsync(
                tenant.Id,
                pageNumber: 1,
                pageSize: 20,
                role: null,
                isActive: null,
                search: "lUcAs");

        var user =
            Assert.Single(result.Items);

        Assert.Equal(
            firstUser.PublicId,
            user.PublicId);

        Assert.Equal(
            1,
            result.TotalCount);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
GetPagedAsync_ShouldFilterUsersByEmailSearch()
    {
        await using var context =
            _database.CreateDbContext();

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var uniqueValue =
            Guid.NewGuid().ToString("N");

        var tenant = new Tenant(
            "Empresa dos Usuários",
            $"registration-{uniqueValue}",
            $"tenant-{uniqueValue}@test.local");

        context.Tenants.Add(tenant);

        await context.SaveChangesAsync();

        var firstUser = new User(
            tenant.Id,
            "Primeiro Usuário",
            $"financeiro-{uniqueValue}@empresa.local",
            "password-hash-for-test",
            UserRole.Member);

        var secondUser = new User(
            tenant.Id,
            "Segundo Usuário",
            $"suporte-{uniqueValue}@empresa.local",
            "password-hash-for-test",
            UserRole.Member);

        context.Users.Add(firstUser);
        context.Users.Add(secondUser);

        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var repository =
            new UserRepository(context);

        var result =
            await repository.GetPagedAsync(
                tenant.Id,
                pageNumber: 1,
                pageSize: 20,
                role: null,
                isActive: null,
                search: "FiNaNcEiRo");

        var user =
            Assert.Single(result.Items);

        Assert.Equal(
            firstUser.PublicId,
            user.PublicId);

        Assert.Equal(
            1,
            result.TotalCount);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
    GetPagedAsync_ShouldPaginateAndOrderByNameThenId()
    {
        await using var context =
            _database.CreateDbContext();

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var uniqueValue =
            Guid.NewGuid().ToString("N");

        var tenant = new Tenant(
            "Empresa dos Usuários",
            $"registration-{uniqueValue}",
            $"tenant-{uniqueValue}@test.local");

        context.Tenants.Add(tenant);

        await context.SaveChangesAsync();

        var firstAna = new User(
            tenant.Id,
            "Ana",
            $"ana-1-{uniqueValue}@test.local",
            "password-hash-for-test",
            UserRole.Member);

        context.Users.Add(firstAna);
        await context.SaveChangesAsync();

        var secondAna = new User(
            tenant.Id,
            "Ana",
            $"ana-2-{uniqueValue}@test.local",
            "password-hash-for-test",
            UserRole.Member);

        context.Users.Add(secondAna);
        await context.SaveChangesAsync();

        var bruno = new User(
            tenant.Id,
            "Bruno",
            $"bruno-{uniqueValue}@test.local",
            "password-hash-for-test",
            UserRole.Member);

        context.Users.Add(bruno);
        await context.SaveChangesAsync();

        var carlos = new User(
            tenant.Id,
            "Carlos",
            $"carlos-{uniqueValue}@test.local",
            "password-hash-for-test",
            UserRole.Member);

        context.Users.Add(carlos);
        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var repository =
            new UserRepository(context);

        var firstPage =
            await repository.GetPagedAsync(
                tenant.Id,
                pageNumber: 1,
                pageSize: 2,
                role: null,
                isActive: null,
                search: null);

        var secondPage =
            await repository.GetPagedAsync(
                tenant.Id,
                pageNumber: 2,
                pageSize: 2,
                role: null,
                isActive: null,
                search: null);

        Assert.Equal(
            4,
            firstPage.TotalCount);

        Assert.Equal(
            4,
            secondPage.TotalCount);

        Assert.Equal(
            new[]
            {
            firstAna.PublicId,
            secondAna.PublicId
            },
            firstPage.Items
                .Select(user => user.PublicId)
                .ToArray());

        Assert.Equal(
            new[]
            {
            bruno.PublicId,
            carlos.PublicId
            },
            secondPage.Items
                .Select(user => user.PublicId)
                .ToArray());

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
GetPagedAsync_ShouldCombineRoleIsActiveAndSearchFilters()
    {
        await using var context =
            _database.CreateDbContext();

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var uniqueValue =
            Guid.NewGuid().ToString("N");

        var tenant = new Tenant(
            "Empresa dos Usuários",
            $"registration-{uniqueValue}",
            $"tenant-{uniqueValue}@test.local");

        context.Tenants.Add(tenant);

        await context.SaveChangesAsync();

        var matchingUser = new User(
            tenant.Id,
            "Lucas Gerente",
            $"lucas-manager-{uniqueValue}@test.local",
            "password-hash-for-test",
            UserRole.ProjectManager);

        matchingUser.Deactivate();

        var wrongRole = new User(
            tenant.Id,
            "Lucas Membro",
            $"lucas-member-{uniqueValue}@test.local",
            "password-hash-for-test",
            UserRole.Member);

        wrongRole.Deactivate();

        var wrongStatus = new User(
            tenant.Id,
            "Lucas Gerente Ativo",
            $"lucas-active-{uniqueValue}@test.local",
            "password-hash-for-test",
            UserRole.ProjectManager);

        var wrongSearch = new User(
            tenant.Id,
            "Maria Gerente",
            $"maria-manager-{uniqueValue}@test.local",
            "password-hash-for-test",
            UserRole.ProjectManager);

        wrongSearch.Deactivate();

        context.Users.AddRange(
            matchingUser,
            wrongRole,
            wrongStatus,
            wrongSearch);

        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var repository =
            new UserRepository(context);

        var result =
            await repository.GetPagedAsync(
                tenant.Id,
                pageNumber: 1,
                pageSize: 20,
                role: UserRole.ProjectManager,
                isActive: false,
                search: "lucas");

        var user =
            Assert.Single(result.Items);

        Assert.Equal(
            matchingUser.PublicId,
            user.PublicId);

        Assert.Equal(
            1,
            result.TotalCount);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
    GetTrackedByPublicIdAsync_ShouldTrackAndPersistUserChanges()
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
            "Usuário Original",
            $"original-{uniqueValue}@test.local",
            "password-hash-for-test",
            UserRole.Member);

        context.Users.Add(user);

        await context.SaveChangesAsync();

        var userPublicId =
            user.PublicId;

        context.ChangeTracker.Clear();

        var repository =
            new UserRepository(context);

        var trackedUser =
            await repository.GetTrackedByPublicIdAsync(
                tenant.Id,
                userPublicId);

        Assert.NotNull(
            trackedUser);

        Assert.Equal(
            EntityState.Unchanged,
            context.Entry(trackedUser!).State);

        trackedUser.Rename(
            "Usuário Atualizado");

        trackedUser.ChangeEmail(
            $"updated-{uniqueValue}@test.local");

        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var persistedUser =
            await context.Users
                .AsNoTracking()
                .SingleAsync(
                    currentUser =>
                        currentUser.PublicId ==
                        userPublicId);

        Assert.Equal(
            "Usuário Atualizado",
            persistedUser.Name);

        Assert.Equal(
            $"updated-{uniqueValue}@test.local",
            persistedUser.Email);

        Assert.NotNull(
            persistedUser.UpdatedAt);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
    GetTrackedByPublicIdAsync_ShouldReturnNull_WhenUserBelongsToDifferentTenant()
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
            $"first-{uniqueValue}@test.local");

        var secondTenant = new Tenant(
            "Segunda Empresa",
            $"registration-second-{uniqueValue}",
            $"second-{uniqueValue}@test.local");

        context.Tenants.Add(firstTenant);
        context.Tenants.Add(secondTenant);

        await context.SaveChangesAsync();

        var user = new User(
            secondTenant.Id,
            "Usuário da Segunda Empresa",
            $"user-{uniqueValue}@test.local",
            "password-hash-for-test",
            UserRole.Member);

        context.Users.Add(user);

        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var repository =
            new UserRepository(context);

        var result =
            await repository.GetTrackedByPublicIdAsync(
                firstTenant.Id,
                user.PublicId);

        Assert.Null(
            result);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
    GetTrackedByPublicIdAsync_ShouldPersistUserDeactivation()
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
            UserRole.Member);

        context.Users.Add(user);

        await context.SaveChangesAsync();

        var userPublicId =
            user.PublicId;

        context.ChangeTracker.Clear();

        var repository =
            new UserRepository(context);

        var trackedUser =
            await repository.GetTrackedByPublicIdAsync(
                tenant.Id,
                userPublicId);

        Assert.NotNull(
            trackedUser);

        trackedUser!.Deactivate();

        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var persistedUser =
            await context.Users
                .AsNoTracking()
                .SingleAsync(
                    currentUser =>
                        currentUser.PublicId ==
                        userPublicId);

        Assert.False(
            persistedUser.IsActive);

        Assert.NotNull(
            persistedUser.UpdatedAt);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
    GetTrackedByPublicIdAsync_ShouldPersistUserRoleChange()
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
            UserRole.Member);

        context.Users.Add(user);

        await context.SaveChangesAsync();

        var userPublicId =
            user.PublicId;

        context.ChangeTracker.Clear();

        var repository =
            new UserRepository(context);

        var trackedUser =
            await repository.GetTrackedByPublicIdAsync(
                tenant.Id,
                userPublicId);

        Assert.NotNull(
            trackedUser);

        trackedUser!.ChangeRole(
            UserRole.ProjectManager);

        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var persistedUser =
            await context.Users
                .AsNoTracking()
                .SingleAsync(
                    currentUser =>
                        currentUser.PublicId ==
                        userPublicId);

        Assert.Equal(
            UserRole.ProjectManager,
            persistedUser.Role);

        Assert.NotNull(
            persistedUser.UpdatedAt);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
GetByEmailAsync_ShouldReturnUser_WhenEmailDiffersOnlyByCase()
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

        var userPublicId =
            user.PublicId;

        context.ChangeTracker.Clear();

        var repository =
            new UserRepository(context);

        var result =
            await repository.GetByEmailAsync(
                tenant.Id,
                email.ToUpperInvariant());

        Assert.NotNull(
            result);

        Assert.Equal(
            userPublicId,
            result!.PublicId);

        Assert.Equal(
            tenant.Id,
            result.TenantId);

        Assert.Equal(
            email,
            result.Email);

        Assert.Equal(
            "password-hash-for-test",
            result.PasswordHash);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
    GetByEmailAsync_ShouldReturnNull_WhenUserBelongsToDifferentTenant()
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
            $"first-{uniqueValue}@test.local");

        var secondTenant = new Tenant(
            "Segunda Empresa",
            $"registration-second-{uniqueValue}",
            $"second-{uniqueValue}@test.local");

        context.Tenants.Add(firstTenant);
        context.Tenants.Add(secondTenant);

        await context.SaveChangesAsync();

        var email =
            $"usuario-{uniqueValue}@test.local";

        var user = new User(
            secondTenant.Id,
            "Usuário da Segunda Empresa",
            email,
            "password-hash-for-test",
            UserRole.Member);

        context.Users.Add(user);

        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var repository =
            new UserRepository(context);

        var result =
            await repository.GetByEmailAsync(
                firstTenant.Id,
                email);

        Assert.Null(
            result);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
    GetSystemAdminByEmailAsync_ShouldReturnSystemAdmin_WhenEmailDiffersOnlyByCase()
    {
        await using var context =
            _database.CreateDbContext();

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var uniqueValue =
            Guid.NewGuid().ToString("N");

        var email =
            $"admin-{uniqueValue}@test.local";

        var tenant =
            new Tenant(
                "Empresa de Teste",
                $"registration-{uniqueValue}",
                $"tenant-{uniqueValue}@test.local");

        context.Tenants.Add(tenant);

        await context.SaveChangesAsync();

        var tenantUser =
            new User(
                tenant.Id,
                "Usuário do Tenant",
                email,
                "tenant-user-password-hash",
                UserRole.Member);

        var systemAdmin =
            new User(
                null,
                "Administrador do Sistema",
                email.ToUpperInvariant(),
                "system-admin-password-hash",
                UserRole.SystemAdmin);

        context.Users.Add(tenantUser);
        context.Users.Add(systemAdmin);

        await context.SaveChangesAsync();

        var systemAdminPublicId =
            systemAdmin.PublicId;

        context.ChangeTracker.Clear();

        var repository =
            new UserRepository(context);

        var result =
            await repository.GetSystemAdminByEmailAsync(
                email);

        Assert.NotNull(
            result);

        Assert.Equal(
            systemAdminPublicId,
            result!.PublicId);

        Assert.Null(
            result.TenantId);

        Assert.Equal(
            UserRole.SystemAdmin,
            result.Role);

        Assert.Equal(
            email.ToUpperInvariant(),
            result.Email);

        Assert.Equal(
            "system-admin-password-hash",
            result.PasswordHash);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
    GetSystemAdminByEmailAsync_ShouldReturnNull_WhenOnlyTenantUserExists()
    {
        await using var context =
            _database.CreateDbContext();

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var uniqueValue =
            Guid.NewGuid().ToString("N");

        var tenant =
            new Tenant(
                "Empresa de Teste",
                $"registration-{uniqueValue}",
                $"tenant-{uniqueValue}@test.local");

        context.Tenants.Add(tenant);

        await context.SaveChangesAsync();

        var email =
            $"usuario-{uniqueValue}@test.local";

        var tenantUser =
            new User(
                tenant.Id,
                "Usuário do Tenant",
                email,
                "tenant-user-password-hash",
                UserRole.TenantAdmin);

        context.Users.Add(tenantUser);

        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var repository =
            new UserRepository(context);

        var result =
            await repository.GetSystemAdminByEmailAsync(
                email);

        Assert.Null(
            result);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
    SaveChanges_ShouldThrow_WhenSystemAdminEmailDiffersOnlyByCase()
    {
        await using var context =
            _database.CreateDbContext();

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var uniqueValue =
            Guid.NewGuid().ToString("N");

        var email =
            $"admin-{uniqueValue}@test.local";

        var firstSystemAdmin =
            new User(
                null,
                "Primeiro Administrador",
                email,
                "first-password-hash",
                UserRole.SystemAdmin);

        var secondSystemAdmin =
            new User(
                null,
                "Segundo Administrador",
                email.ToUpperInvariant(),
                "second-password-hash",
                UserRole.SystemAdmin);

        context.Users.Add(firstSystemAdmin);

        await context.SaveChangesAsync();

        context.Users.Add(secondSystemAdmin);

        await Assert.ThrowsAsync<DbUpdateException>(
            () => context.SaveChangesAsync());

        await transaction.RollbackAsync();
    }
}