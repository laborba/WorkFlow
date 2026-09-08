using Microsoft.EntityFrameworkCore;
using WorkFlow.Domain.Entities;
using WorkFlow.Domain.Enums;
using WorkFlow.Infrastructure.Persistence.Repositories;
using WorkFlow.IntegrationTests.Infrastructure;

namespace WorkFlow.IntegrationTests.Persistence;

[Collection(TestCollectionNames.PostgreSql)]
public sealed class UpdateProjectPersistenceTests
{
    private readonly PostgreSqlTestDatabase _database;

    public UpdateProjectPersistenceTests(
        PostgreSqlTestDatabase database)
    {
        _database = database;
    }

    [Fact]
    public async Task
    GetTrackedByPublicIdAsync_ShouldPersistProjectChanges()
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

        var creator =
            new User(
                tenant.Id,
                "Criador",
                $"creator-{uniqueValue}@test.local",
                "password-hash",
                UserRole.TenantAdmin);

        context.Users.Add(
            creator);

        await context.SaveChangesAsync();

        var originalDueDate =
            new DateTime(
                2026,
                10,
                10,
                12,
                0,
                0,
                DateTimeKind.Utc);

        var project =
            new Project(
                tenant.Id,
                "Projeto original",
                creator.Id,
                "Descrição original",
                dueDate: originalDueDate);

        context.Projects.Add(
            project);

        await context.SaveChangesAsync();

        var projectPublicId =
            project.PublicId;

        context.ChangeTracker.Clear();

        var repository =
            new ProjectRepository(
                context);

        var trackedProject =
            await repository.GetTrackedByPublicIdAsync(
                tenant.Id,
                projectPublicId);

        Assert.NotNull(
            trackedProject);

        trackedProject!.Rename(
            "Projeto atualizado");

        trackedProject.UpdateDescription(
            "Descrição atualizada");

        trackedProject.ChangeDueDate(
            null);

        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var persistedProject =
            await context.Projects
                .AsNoTracking()
                .SingleAsync(
                    currentProject =>
                        currentProject.PublicId ==
                        projectPublicId);

        Assert.Equal(
            "Projeto atualizado",
            persistedProject.Name);

        Assert.Equal(
            "Descrição atualizada",
            persistedProject.Description);

        Assert.Null(
            persistedProject.DueDate);

        Assert.NotNull(
            persistedProject.UpdatedAt);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
    GetTrackedByPublicIdAsync_ShouldReturnNull_WhenProjectBelongsToAnotherTenant()
    {
        await using var context =
            _database.CreateDbContext();

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var uniqueValue =
            Guid.NewGuid().ToString("N");

        var tenantA =
            new Tenant(
                $"Tenant A {uniqueValue}",
                $"REG-A-{uniqueValue}",
                $"tenant-a-{uniqueValue}@test.local");

        var tenantB =
            new Tenant(
                $"Tenant B {uniqueValue}",
                $"REG-B-{uniqueValue}",
                $"tenant-b-{uniqueValue}@test.local");

        context.Tenants.AddRange(
            tenantA,
            tenantB);

        await context.SaveChangesAsync();

        var creator =
            new User(
                tenantA.Id,
                "Criador",
                $"creator-{uniqueValue}@test.local",
                "password-hash",
                UserRole.TenantAdmin);

        context.Users.Add(
            creator);

        await context.SaveChangesAsync();

        var project =
            new Project(
                tenantA.Id,
                "Projeto Tenant A",
                creator.Id);

        context.Projects.Add(
            project);

        await context.SaveChangesAsync();

        var projectPublicId =
            project.PublicId;

        context.ChangeTracker.Clear();

        var repository =
            new ProjectRepository(
                context);

        var result =
            await repository.GetTrackedByPublicIdAsync(
                tenantB.Id,
                projectPublicId);

        Assert.Null(
            result);

        await transaction.RollbackAsync();
    }
}