using Microsoft.EntityFrameworkCore;
using WorkFlow.Domain.Entities;
using WorkFlow.Domain.Enums;
using WorkFlow.Infrastructure.Persistence.Repositories;
using WorkFlow.IntegrationTests.Infrastructure;

namespace WorkFlow.IntegrationTests.Persistence.Projects;

[Collection(TestCollectionNames.PostgreSql)]
public sealed class AddProjectMemberPersistenceTests
{
    private readonly PostgreSqlTestDatabase _database;

    public AddProjectMemberPersistenceTests(
        PostgreSqlTestDatabase database)
    {
        _database = database;
    }

    [Fact]
    public async Task
    AddAsync_ShouldRejectSecondActiveMembershipForSameProjectAndUser()
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

        var addedByUser =
            new User(
                tenant.Id,
                "Administrador",
                $"admin-{uniqueValue}@test.local",
                "password-hash",
                UserRole.TenantAdmin);

        var memberUser =
            new User(
                tenant.Id,
                "Membro",
                $"member-{uniqueValue}@test.local",
                "password-hash",
                UserRole.Member);

        context.Users.AddRange(
            addedByUser,
            memberUser);

        await context.SaveChangesAsync();

        var project =
            new Project(
                tenant.Id,
                $"Projeto {uniqueValue}",
                addedByUser.Id);

        context.Projects.Add(
            project);

        await context.SaveChangesAsync();

        var repository =
            new ProjectMemberRepository(
                context);

        var firstMembership =
            new ProjectMember(
                project.Id,
                memberUser.Id,
                addedByUser.Id);

        await repository.AddAsync(
            firstMembership);

        await context.SaveChangesAsync();

        var secondMembership =
            new ProjectMember(
                project.Id,
                memberUser.Id,
                addedByUser.Id);

        await repository.AddAsync(
            secondMembership);

        await Assert.ThrowsAsync<DbUpdateException>(
            () => context.SaveChangesAsync());

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
    AddAsync_ShouldAllowNewMembershipAfterPreviousMembershipWasRemoved()
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

        var addedByUser =
            new User(
                tenant.Id,
                "Administrador",
                $"admin-{uniqueValue}@test.local",
                "password-hash",
                UserRole.TenantAdmin);

        var memberUser =
            new User(
                tenant.Id,
                "Membro",
                $"member-{uniqueValue}@test.local",
                "password-hash",
                UserRole.Member);

        context.Users.AddRange(
            addedByUser,
            memberUser);

        await context.SaveChangesAsync();

        var project =
            new Project(
                tenant.Id,
                $"Projeto {uniqueValue}",
                addedByUser.Id);

        context.Projects.Add(
            project);

        await context.SaveChangesAsync();

        var repository =
            new ProjectMemberRepository(
                context);

        var firstMembership =
            new ProjectMember(
                project.Id,
                memberUser.Id,
                addedByUser.Id);

        await repository.AddAsync(
            firstMembership);

        await context.SaveChangesAsync();

        firstMembership.Remove();

        await context.SaveChangesAsync();

        var secondMembership =
            new ProjectMember(
                project.Id,
                memberUser.Id,
                addedByUser.Id);

        await repository.AddAsync(
            secondMembership);

        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var persistedMemberships =
            await context.ProjectMembers
                .AsNoTracking()
                .Where(member =>
                    member.ProjectId == project.Id &&
                    member.UserId == memberUser.Id)
                .OrderBy(member => member.Id)
                .ToListAsync();

        Assert.Equal(
            2,
            persistedMemberships.Count);

        Assert.Single(
            persistedMemberships.Where(
                member =>
                    member.RemovedAt is not null));

        Assert.Single(
            persistedMemberships.Where(
                member =>
                    member.RemovedAt is null));

        var isActiveMember =
            await repository.IsActiveMemberAsync(
                project.Id,
                memberUser.Id);

        Assert.True(
            isActiveMember);

        await transaction.RollbackAsync();
    }
}