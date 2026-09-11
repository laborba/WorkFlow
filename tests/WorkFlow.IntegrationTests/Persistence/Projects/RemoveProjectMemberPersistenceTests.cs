using Microsoft.EntityFrameworkCore;
using WorkFlow.Domain.Entities;
using WorkFlow.Domain.Enums;
using WorkFlow.Infrastructure.Persistence.Repositories;
using WorkFlow.IntegrationTests.Infrastructure;

namespace WorkFlow.IntegrationTests.Persistence.Projects;

[Collection(TestCollectionNames.PostgreSql)]
public sealed class RemoveProjectMemberPersistenceTests
{
    private readonly PostgreSqlTestDatabase _database;

    public RemoveProjectMemberPersistenceTests(
        PostgreSqlTestDatabase database)
    {
        _database = database;
    }

    [Fact]
    public async Task
    GetActiveForUpdateAsync_ShouldReturnTrackedMemberAndPersistRemoval()
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

        var member =
            new User(
                tenant.Id,
                "Membro",
                $"member-{uniqueValue}@test.local",
                "password-hash",
                UserRole.Member);

        context.Users.AddRange(
            admin,
            member);

        await context.SaveChangesAsync();

        var project =
            new Project(
                tenant.Id,
                $"Projeto {uniqueValue}",
                admin.Id);

        context.Projects.Add(
            project);

        await context.SaveChangesAsync();

        var projectMember =
            new ProjectMember(
                project.Id,
                member.Id,
                admin.Id);

        context.ProjectMembers.Add(
            projectMember);

        await context.SaveChangesAsync();

        var repository =
            new ProjectMemberRepository(
                context);

        var trackedMember =
            await repository.GetActiveForUpdateAsync(
                project.Id,
                member.Id);

        Assert.NotNull(
            trackedMember);

        Assert.Null(
            trackedMember.RemovedAt);

        trackedMember.Remove();

        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var persistedMember =
            await context.ProjectMembers
                .AsNoTracking()
                .SingleAsync(
                    item =>
                        item.ProjectId == project.Id &&
                        item.UserId == member.Id);

        Assert.NotNull(
            persistedMember.RemovedAt);

        var isActive =
            await repository.IsActiveMemberAsync(
                project.Id,
                member.Id);

        Assert.False(
            isActive);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
    GetActiveForUpdateAsync_ShouldReturnNull_WhenMembershipWasAlreadyRemoved()
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

        var member =
            new User(
                tenant.Id,
                "Membro",
                $"member-{uniqueValue}@test.local",
                "password-hash",
                UserRole.Member);

        context.Users.AddRange(
            admin,
            member);

        await context.SaveChangesAsync();

        var project =
            new Project(
                tenant.Id,
                $"Projeto {uniqueValue}",
                admin.Id);

        context.Projects.Add(
            project);

        await context.SaveChangesAsync();

        var projectMember =
            new ProjectMember(
                project.Id,
                member.Id,
                admin.Id);

        projectMember.Remove();

        context.ProjectMembers.Add(
            projectMember);

        await context.SaveChangesAsync();

        var repository =
            new ProjectMemberRepository(
                context);

        var result =
            await repository.GetActiveForUpdateAsync(
                project.Id,
                member.Id);

        Assert.Null(
            result);

        await transaction.RollbackAsync();
    }
}