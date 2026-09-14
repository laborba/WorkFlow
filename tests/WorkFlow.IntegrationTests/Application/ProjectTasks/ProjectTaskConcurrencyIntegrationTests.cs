using Microsoft.EntityFrameworkCore;
using WorkFlow.Application.Projects;
using WorkFlow.Application.Projects.ArchiveProject;
using WorkFlow.Application.Projects.CompleteProject;
using WorkFlow.Application.ProjectTasks;
using WorkFlow.Application.ProjectTasks.CreateProjectTask;
using WorkFlow.Domain.Entities;
using WorkFlow.Domain.Enums;
using WorkFlow.Infrastructure.Persistence;
using WorkFlow.Infrastructure.Persistence.Repositories;
using WorkFlow.IntegrationTests.Infrastructure;

namespace WorkFlow.IntegrationTests.Application.ProjectTasks;

[Collection(TestCollectionNames.PostgreSql)]
public sealed class ProjectTaskConcurrencyIntegrationTests
{
    private readonly PostgreSqlTestDatabase _database;

    public ProjectTaskConcurrencyIntegrationTests(
        PostgreSqlTestDatabase database)
    {
        _database =
            database;
    }

    [Fact]
    public async Task
    CompleteProject_ShouldSeeNewBacklogTask_WhenTaskCreationWinsProjectLock()
    {
        var scenario =
            await CreateScenarioAsync(
                ProjectStatus.InProgress,
                includeClosedTask: true);

        try
        {
            await using var createContext =
                _database.CreateDbContext();

            await using var createTransaction =
                await createContext.Database
                    .BeginTransactionAsync();

            var lockedProject =
                await new ProjectRepository(createContext)
                    .GetForUpdateByPublicIdAsync(
                        scenario.TenantId,
                        scenario.ProjectPublicId);

            Assert.NotNull(
                lockedProject);

            await using var completeContext =
                _database.CreateDbContext();

            var completeTask =
                CreateCompleteProjectHandler(
                    completeContext)
                .HandleAsync(
                    new CompleteProjectCommand(
                        scenario.TenantPublicId,
                        scenario.ProjectPublicId,
                        scenario.UserPublicId));

            await Task.Delay(
                100);

            var createResult =
                await CreateProjectTaskHandler(
                    createContext)
                .HandleAsync(
                    new CreateProjectTaskCommand(
                        scenario.TenantPublicId,
                        scenario.ProjectPublicId,
                        scenario.UserPublicId,
                        "Tarefa criada durante concorrência",
                        null,
                        ProjectTaskPriority.Medium,
                        null));

            Assert.True(
                createResult.IsSuccess);

            Assert.Equal(
                ProjectTaskStatus.Backlog,
                createResult.Value!.Status);

            await createTransaction.CommitAsync();

            var completeResult =
                await completeTask.WaitAsync(
                    TimeSpan.FromSeconds(10));

            Assert.True(
                completeResult.IsFailure);

            Assert.Equal(
                ProjectErrors.HasOpenTasks,
                completeResult.Error);

            await using var verificationContext =
                _database.CreateDbContext();

            var persistedProject =
                await verificationContext.Projects
                    .AsNoTracking()
                    .SingleAsync(
                        project =>
                            project.PublicId ==
                            scenario.ProjectPublicId);

            Assert.Equal(
                ProjectStatus.InProgress,
                persistedProject.Status);

            Assert.True(
                await verificationContext.ProjectTasks
                    .AsNoTracking()
                    .AnyAsync(
                        task =>
                            task.ProjectId ==
                                persistedProject.Id &&
                            task.Status ==
                                ProjectTaskStatus.Backlog));
        }
        finally
        {
            await CleanupScenarioAsync(
                scenario.TenantId);
        }
    }

    [Fact]
    public async Task
    CreateProjectTask_ShouldRejectCreation_WhenArchiveWinsProjectLock()
    {
        var scenario =
            await CreateScenarioAsync(
                ProjectStatus.Planning,
                includeClosedTask: false);

        try
        {
            await using var archiveContext =
                _database.CreateDbContext();

            await using var archiveTransaction =
                await archiveContext.Database
                    .BeginTransactionAsync();

            var lockedProject =
                await new ProjectRepository(archiveContext)
                    .GetForUpdateByPublicIdAsync(
                        scenario.TenantId,
                        scenario.ProjectPublicId);

            Assert.NotNull(
                lockedProject);

            await using var createContext =
                _database.CreateDbContext();

            var createTask =
                CreateProjectTaskHandler(
                    createContext)
                .HandleAsync(
                    new CreateProjectTaskCommand(
                        scenario.TenantPublicId,
                        scenario.ProjectPublicId,
                        scenario.UserPublicId,
                        "Tarefa concorrente",
                        null,
                        ProjectTaskPriority.Medium,
                        null));

            await Task.Delay(
                100);

            var archiveResult =
                await CreateArchiveProjectHandler(
                    archiveContext)
                .HandleAsync(
                    new ArchiveProjectCommand(
                        scenario.TenantPublicId,
                        scenario.ProjectPublicId,
                        scenario.UserPublicId));

            Assert.True(
                archiveResult.IsSuccess);

            Assert.Equal(
                ProjectStatus.Archived,
                archiveResult.Value!.Status);

            await archiveTransaction.CommitAsync();

            var createResult =
                await createTask.WaitAsync(
                    TimeSpan.FromSeconds(10));

            Assert.True(
                createResult.IsFailure);

            Assert.Equal(
                ProjectTaskErrors
                    .CreationBlockedByProjectStatus,
                createResult.Error);

            await using var verificationContext =
                _database.CreateDbContext();

            var persistedProject =
                await verificationContext.Projects
                    .AsNoTracking()
                    .SingleAsync(
                        project =>
                            project.PublicId ==
                            scenario.ProjectPublicId);

            Assert.Equal(
                ProjectStatus.Archived,
                persistedProject.Status);

            Assert.False(
                await verificationContext.ProjectTasks
                    .AsNoTracking()
                    .AnyAsync(
                        task =>
                            task.ProjectId ==
                            persistedProject.Id));
        }
        finally
        {
            await CleanupScenarioAsync(
                scenario.TenantId);
        }
    }

    private CreateProjectTaskHandler
        CreateProjectTaskHandler(
            WorkFlowDbContext context)
    {
        return new CreateProjectTaskHandler(
            new TenantRepository(context),
            new UserRepository(context),
            new ProjectRepository(context),
            new ProjectMemberRepository(context),
            new ProjectMemberPermissionRepository(context),
            new ProjectTaskRepository(context),
            context);
    }

    private CompleteProjectHandler
        CreateCompleteProjectHandler(
            WorkFlowDbContext context)
    {
        return new CompleteProjectHandler(
            new TenantRepository(context),
            new UserRepository(context),
            new ProjectRepository(context),
            new ProjectMemberRepository(context),
            new ProjectMemberPermissionRepository(context),
            new ProjectTaskRepository(context),
            context);
    }

    private ArchiveProjectHandler
        CreateArchiveProjectHandler(
            WorkFlowDbContext context)
    {
        return new ArchiveProjectHandler(
            new TenantRepository(context),
            new UserRepository(context),
            new ProjectRepository(context),
            new ProjectMemberRepository(context),
            new ProjectMemberPermissionRepository(context),
            context);
    }

    private async Task<Scenario>
        CreateScenarioAsync(
            ProjectStatus projectStatus,
            bool includeClosedTask)
    {
        await using var context =
            _database.CreateDbContext();

        var uniqueValue =
            Guid.NewGuid().ToString("N");

        var tenant =
            new Tenant(
                $"Tenant Concurrency {uniqueValue}",
                $"REG-{uniqueValue}",
                $"tenant-{uniqueValue}@test.local");

        context.Tenants.Add(
            tenant);

        await context.SaveChangesAsync();

        var user =
            new User(
                tenant.Id,
                "Administrador Concorrência",
                $"admin-{uniqueValue}@test.local",
                "password-hash",
                UserRole.TenantAdmin);

        context.Users.Add(
            user);

        await context.SaveChangesAsync();

        var project =
            new Project(
                tenant.Id,
                $"Projeto Concorrência {uniqueValue}",
                user.Id);

        if (projectStatus ==
            ProjectStatus.InProgress)
        {
            project.Start();
        }
        else if (projectStatus !=
                 ProjectStatus.Planning)
        {
            throw new ArgumentOutOfRangeException(
                nameof(projectStatus));
        }

        context.Projects.Add(
            project);

        await context.SaveChangesAsync();

        if (includeClosedTask)
        {
            var closedTask =
                new ProjectTask(
                    project.Id,
                    "Tarefa já concluída",
                    ProjectTaskPriority.Medium,
                    user.Id,
                    responsibleUserId:
                        user.Id);

            closedTask.MoveToTodo();
            closedTask.Start();
            closedTask.SendToValidation();
            closedTask.ClaimValidation(
                user.Id);
            closedTask.ApproveValidation(
                user.Id);

            context.ProjectTasks.Add(
                closedTask);

            await context.SaveChangesAsync();
        }

        return new Scenario(
            tenant.Id,
            tenant.PublicId,
            user.PublicId,
            project.PublicId);
    }

    private async Task CleanupScenarioAsync(
        long tenantId)
    {
        await using var context =
            _database.CreateDbContext();

        var projectIds =
            await context.Projects
                .Where(project =>
                    project.TenantId ==
                    tenantId)
                .Select(project =>
                    project.Id)
                .ToArrayAsync();

        if (projectIds.Length > 0)
        {
            await context.ProjectTasks
                .Where(task =>
                    projectIds.Contains(
                        task.ProjectId))
                .ExecuteDeleteAsync();

            await context.Projects
                .Where(project =>
                    project.TenantId ==
                    tenantId)
                .ExecuteDeleteAsync();
        }

        await context.Users
            .Where(user =>
                user.TenantId ==
                tenantId)
            .ExecuteDeleteAsync();

        await context.Tenants
            .Where(tenant =>
                tenant.Id ==
                tenantId)
            .ExecuteDeleteAsync();
    }

    private sealed record Scenario(
        long TenantId,
        Guid TenantPublicId,
        Guid UserPublicId,
        Guid ProjectPublicId);
}