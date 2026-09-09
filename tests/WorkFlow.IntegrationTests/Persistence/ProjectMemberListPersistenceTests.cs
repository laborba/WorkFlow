using WorkFlow.Domain.Entities;
using WorkFlow.Domain.Enums;
using WorkFlow.Infrastructure.Persistence.Repositories;
using WorkFlow.IntegrationTests.Infrastructure;

namespace WorkFlow.IntegrationTests.Persistence;

[Collection(TestCollectionNames.PostgreSql)]
public sealed class ProjectMemberListPersistenceTests
{
    private readonly PostgreSqlTestDatabase _database;

    public ProjectMemberListPersistenceTests(
        PostgreSqlTestDatabase database)
    {
        _database = database;
    }

    [Fact]
    public async Task
    GetPagedAsync_ShouldReturnOnlyActiveMembersAndResolveAddedByUser()
    {
        await using var context =
            _database.CreateDbContext();

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var uniqueValue =
            Guid.NewGuid().ToString("N");

        var tenant =
            new Tenant(
                $"Tenant {uniqueValue}",
                $"REG-{uniqueValue}",
                $"tenant-{uniqueValue}@test.local");

        context.Tenants.Add(
            tenant);

        await context.SaveChangesAsync();

        var admin =
            new User(
                tenant.Id,
                "Administrador",
                $"admin-{uniqueValue}@test.local",
                "password-hash",
                UserRole.TenantAdmin);

        var activeUser =
            new User(
                tenant.Id,
                "Membro Ativo",
                $"active-{uniqueValue}@test.local",
                "password-hash",
                UserRole.Member);

        var removedUser =
            new User(
                tenant.Id,
                "Membro Removido",
                $"removed-{uniqueValue}@test.local",
                "password-hash",
                UserRole.Member);

        context.Users.AddRange(
            admin,
            activeUser,
            removedUser);

        await context.SaveChangesAsync();

        var project =
            new Project(
                tenant.Id,
                $"Projeto {uniqueValue}",
                admin.Id);

        context.Projects.Add(
            project);

        await context.SaveChangesAsync();

        var activeMember =
            new ProjectMember(
                project.Id,
                activeUser.Id,
                admin.Id);

        var removedMember =
            new ProjectMember(
                project.Id,
                removedUser.Id,
                admin.Id);

        removedMember.Remove();

        context.ProjectMembers.AddRange(
            activeMember,
            removedMember);

        await context.SaveChangesAsync();

        var repository =
            new ProjectMemberRepository(
                context);

        var result =
            await repository.GetPagedAsync(
                project.Id,
                1,
                20,
                null);

        Assert.Equal(
            1,
            result.TotalCount);

        var item =
            Assert.Single(
                result.Items);

        Assert.Equal(
            activeUser.PublicId,
            item.UserPublicId);

        Assert.Equal(
            activeUser.Name,
            item.Name);

        Assert.Equal(
            activeUser.Email,
            item.Email);

        Assert.Equal(
            activeUser.Role,
            item.Role);

        Assert.Equal(
            admin.PublicId,
            item.AddedByUserPublicId);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
    GetPagedAsync_ShouldSearchByNameOrEmailCaseInsensitive()
    {
        await using var context =
            _database.CreateDbContext();

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var uniqueValue =
            Guid.NewGuid().ToString("N");

        var tenant =
            new Tenant(
                $"Tenant {uniqueValue}",
                $"REG-{uniqueValue}",
                $"tenant-{uniqueValue}@test.local");

        context.Tenants.Add(
            tenant);

        await context.SaveChangesAsync();

        var admin =
            new User(
                tenant.Id,
                "Administrador",
                $"admin-{uniqueValue}@test.local",
                "password-hash",
                UserRole.TenantAdmin);

        var userByName =
            new User(
                tenant.Id,
                "Lucas Especial",
                $"name-{uniqueValue}@test.local",
                "password-hash",
                UserRole.Member);

        var userByEmail =
            new User(
                tenant.Id,
                "Outro Usuário",
                $"BUSCA-{uniqueValue}@example.com",
                "password-hash",
                UserRole.Member);

        context.Users.AddRange(
            admin,
            userByName,
            userByEmail);

        await context.SaveChangesAsync();

        var project =
            new Project(
                tenant.Id,
                $"Projeto {uniqueValue}",
                admin.Id);

        context.Projects.Add(
            project);

        await context.SaveChangesAsync();

        context.ProjectMembers.AddRange(
            new ProjectMember(
                project.Id,
                userByName.Id,
                admin.Id),
            new ProjectMember(
                project.Id,
                userByEmail.Id,
                admin.Id));

        await context.SaveChangesAsync();

        var repository =
            new ProjectMemberRepository(
                context);

        var byName =
            await repository.GetPagedAsync(
                project.Id,
                1,
                20,
                "lUcAs");

        Assert.Equal(
            1,
            byName.TotalCount);

        Assert.Equal(
            userByName.PublicId,
            Assert.Single(byName.Items).UserPublicId);

        var byEmail =
            await repository.GetPagedAsync(
                project.Id,
                1,
                20,
                uniqueValue.ToUpperInvariant());

        Assert.Equal(
            2,
            byEmail.TotalCount);

        Assert.Contains(
            byEmail.Items,
            item =>
                item.UserPublicId ==
                userByName.PublicId);

        Assert.Contains(
            byEmail.Items,
            item =>
                item.UserPublicId ==
                userByEmail.PublicId);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
    GetPagedAsync_ShouldApplyPaginationAndStableOrdering()
    {
        await using var context =
            _database.CreateDbContext();

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var uniqueValue =
            Guid.NewGuid().ToString("N");

        var tenant =
            new Tenant(
                $"Tenant {uniqueValue}",
                $"REG-{uniqueValue}",
                $"tenant-{uniqueValue}@test.local");

        context.Tenants.Add(
            tenant);

        await context.SaveChangesAsync();

        var admin =
            new User(
                tenant.Id,
                "Administrador",
                $"admin-{uniqueValue}@test.local",
                "password-hash",
                UserRole.TenantAdmin);

        var user1 =
            new User(
                tenant.Id,
                "Membro 1",
                $"member1-{uniqueValue}@test.local",
                "password-hash",
                UserRole.Member);

        var user2 =
            new User(
                tenant.Id,
                "Membro 2",
                $"member2-{uniqueValue}@test.local",
                "password-hash",
                UserRole.Member);

        var user3 =
            new User(
                tenant.Id,
                "Membro 3",
                $"member3-{uniqueValue}@test.local",
                "password-hash",
                UserRole.Member);

        context.Users.AddRange(
            admin,
            user1,
            user2,
            user3);

        await context.SaveChangesAsync();

        var project =
            new Project(
                tenant.Id,
                $"Projeto {uniqueValue}",
                admin.Id);

        context.Projects.Add(
            project);

        await context.SaveChangesAsync();

        var member1 =
            new ProjectMember(
                project.Id,
                user1.Id,
                admin.Id);

        await context.ProjectMembers.AddAsync(
            member1);

        await context.SaveChangesAsync();

        var member2 =
            new ProjectMember(
                project.Id,
                user2.Id,
                admin.Id);

        await context.ProjectMembers.AddAsync(
            member2);

        await context.SaveChangesAsync();

        var member3 =
            new ProjectMember(
                project.Id,
                user3.Id,
                admin.Id);

        await context.ProjectMembers.AddAsync(
            member3);

        await context.SaveChangesAsync();

        var repository =
            new ProjectMemberRepository(
                context);

        var firstPage =
            await repository.GetPagedAsync(
                project.Id,
                1,
                2,
                null);

        var secondPage =
            await repository.GetPagedAsync(
                project.Id,
                2,
                2,
                null);

        Assert.Equal(
            3,
            firstPage.TotalCount);

        Assert.Equal(
            3,
            secondPage.TotalCount);

        Assert.Equal(
            2,
            firstPage.Items.Count);

        Assert.Single(
            secondPage.Items);

        var returnedIds =
            firstPage.Items
                .Concat(secondPage.Items)
                .Select(item =>
                    item.UserPublicId)
                .ToArray();

        Assert.Equal(
            3,
            returnedIds.Distinct().Count());

        Assert.Equal(
            user1.PublicId,
            returnedIds[0]);

        Assert.Equal(
            user2.PublicId,
            returnedIds[1]);

        Assert.Equal(
            user3.PublicId,
            returnedIds[2]);

        await transaction.RollbackAsync();
    }
}