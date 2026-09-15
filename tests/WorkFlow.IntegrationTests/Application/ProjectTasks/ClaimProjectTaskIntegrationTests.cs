using Microsoft.EntityFrameworkCore;
using WorkFlow.Application.Abstractions.Persistence;
using WorkFlow.Application.ProjectTasks;
using WorkFlow.Application.ProjectTasks.ClaimProjectTask;
using WorkFlow.Application.Users;
using WorkFlow.Domain.Entities;
using WorkFlow.Domain.Enums;
using WorkFlow.Infrastructure.Persistence;
using WorkFlow.Infrastructure.Persistence.Repositories;
using WorkFlow.IntegrationTests.Infrastructure;

namespace WorkFlow.IntegrationTests.Application.ProjectTasks;

[Collection(TestCollectionNames.PostgreSql)]
public sealed class ClaimProjectTaskIntegrationTests
{
    private readonly PostgreSqlTestDatabase _database;

    public ClaimProjectTaskIntegrationTests(
        PostgreSqlTestDatabase database)
    {
        _database = database;
    }

    [Theory]
    [InlineData(
        UserRole.TenantAdmin,
        ProjectStatus.Planning,
        ProjectTaskStatus.Backlog)]
    [InlineData(
        UserRole.TenantAdmin,
        ProjectStatus.InProgress,
        ProjectTaskStatus.Todo)]
    [InlineData(
        UserRole.TenantAdmin,
        ProjectStatus.Paused,
        ProjectTaskStatus.Paused)]
    [InlineData(
        UserRole.ProjectManager,
        ProjectStatus.Planning,
        ProjectTaskStatus.Backlog)]
    [InlineData(
        UserRole.Member,
        ProjectStatus.Paused,
        ProjectTaskStatus.Todo)]
    public async Task
        HandleAsync_ShouldPersistClaim_WhenAuthorized(
            UserRole requesterRole,
            ProjectStatus projectStatus,
            ProjectTaskStatus taskStatus)
    {
        await using var context =
            _database.CreateDbContext();

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var scenario =
            await CreateScenarioAsync(
                context,
                requesterRole,
                projectStatus,
                taskStatus);

        context.ChangeTracker.Clear();

        var before =
            DateTime.UtcNow;

        var result =
            await CreateHandler(context)
                .HandleAsync(
                    CreateCommand(scenario));

        Assert.True(
            result.IsSuccess);

        Assert.Null(
            result.Error);

        context.ChangeTracker.Clear();

        var persistedTask =
            await context.ProjectTasks
                .AsNoTracking()
                .SingleAsync(
                    task =>
                        task.PublicId ==
                        scenario.Task.PublicId);

        Assert.Equal(
            scenario.Requester.Id,
            persistedTask.ResponsibleUserId);

        Assert.Equal(
            taskStatus,
            persistedTask.Status);

        Assert.NotNull(
            persistedTask.UpdatedAt);

        Assert.InRange(
            persistedTask.UpdatedAt!.Value,
            before,
            DateTime.UtcNow);

        Assert.Equal(
            scenario.Task.PublicId,
            result.Value!.PublicId);

        Assert.Equal(
            scenario.Tenant.PublicId,
            result.Value.TenantPublicId);

        Assert.Equal(
            scenario.Project.PublicId,
            result.Value.ProjectPublicId);

        Assert.Equal(
            scenario.Requester.PublicId,
            result.Value.ResponsibleUserPublicId);

        Assert.Equal(
            taskStatus,
            result.Value.Status);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
        HandleAsync_ShouldRejectTenantAdmin_WhenNotActiveMember()
    {
        await using var context =
            _database.CreateDbContext();

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var scenario =
            await CreateScenarioAsync(
                context,
                UserRole.TenantAdmin,
                ProjectStatus.Planning);

        scenario.RequesterMember.Remove();

        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var result =
            await CreateHandler(context)
                .HandleAsync(
                    CreateCommand(scenario));

        Assert.Equal(
            ProjectTaskErrors.ClaimNotAllowed,
            result.Error);

        context.ChangeTracker.Clear();

        var persistedTask =
            await GetTaskAsync(
                context,
                scenario.Task.PublicId);

        Assert.Null(
            persistedTask.ResponsibleUserId);

        await transaction.RollbackAsync();
    }

    [Theory]
    [InlineData(UserRole.ProjectManager)]
    [InlineData(UserRole.Member)]
    public async Task
        HandleAsync_ShouldRejectClaim_WhenClaimTaskPermissionWasRevoked(
            UserRole requesterRole)
    {
        await using var context =
            _database.CreateDbContext();

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var scenario =
            await CreateScenarioAsync(
                context,
                requesterRole,
                ProjectStatus.Planning);

        scenario.RequesterPermission!.Revoke(
            scenario.Requester.Id);

        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var result =
            await CreateHandler(context)
                .HandleAsync(
                    CreateCommand(scenario));

        Assert.Equal(
            ProjectTaskErrors.ClaimNotAllowed,
            result.Error);

        context.ChangeTracker.Clear();

        var persistedTask =
            await GetTaskAsync(
                context,
                scenario.Task.PublicId);

        Assert.Null(
            persistedTask.ResponsibleUserId);

        await transaction.RollbackAsync();
    }

    [Theory]
    [InlineData(UserRole.TenantAdmin)]
    [InlineData(UserRole.ProjectManager)]
    [InlineData(UserRole.Member)]
    public async Task
        HandleAsync_ShouldRejectClaim_WhenMembershipWasRemoved(
            UserRole requesterRole)
    {
        await using var context =
            _database.CreateDbContext();

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var scenario =
            await CreateScenarioAsync(
                context,
                requesterRole,
                ProjectStatus.Planning);

        scenario.RequesterMember.Remove();

        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var result =
            await CreateHandler(context)
                .HandleAsync(
                    CreateCommand(scenario));

        Assert.Equal(
            ProjectTaskErrors.ClaimNotAllowed,
            result.Error);

        context.ChangeTracker.Clear();

        var persistedTask =
            await GetTaskAsync(
                context,
                scenario.Task.PublicId);

        Assert.Null(
            persistedTask.ResponsibleUserId);

        await transaction.RollbackAsync();
    }

    [Theory]
    [InlineData(ProjectStatus.Completed)]
    [InlineData(ProjectStatus.Archived)]
    public async Task
        HandleAsync_ShouldRejectClaim_WhenProjectStatusBlocksIt(
            ProjectStatus projectStatus)
    {
        await using var context =
            _database.CreateDbContext();

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var scenario =
            await CreateScenarioAsync(
                context,
                UserRole.TenantAdmin,
                projectStatus);

        context.ChangeTracker.Clear();

        var result =
            await CreateHandler(context)
                .HandleAsync(
                    CreateCommand(scenario));

        Assert.Equal(
            ProjectTaskErrors.ClaimBlockedByProjectStatus,
            result.Error);

        context.ChangeTracker.Clear();

        var persistedTask =
            await GetTaskAsync(
                context,
                scenario.Task.PublicId);

        Assert.Null(
            persistedTask.ResponsibleUserId);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
        HandleAsync_ShouldRejectClaim_WhenTaskIsArchived()
    {
        await using var context =
            _database.CreateDbContext();

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var scenario =
            await CreateScenarioAsync(
                context,
                UserRole.TenantAdmin,
                ProjectStatus.Planning);

        scenario.Task.Cancel();
        scenario.Task.Archive();

        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var result =
            await CreateHandler(context)
                .HandleAsync(
                    CreateCommand(scenario));

        Assert.Equal(
            ProjectTaskErrors.Archived,
            result.Error);

        context.ChangeTracker.Clear();

        var persistedTask =
            await GetTaskAsync(
                context,
                scenario.Task.PublicId);

        Assert.True(
            persistedTask.IsArchived);

        Assert.Null(
            persistedTask.ResponsibleUserId);

        await transaction.RollbackAsync();
    }

    [Theory]
    [InlineData(ProjectTaskStatus.InProgress)]
    [InlineData(ProjectTaskStatus.Validation)]
    [InlineData(ProjectTaskStatus.Done)]
    [InlineData(ProjectTaskStatus.Cancelled)]
    public async Task
        HandleAsync_ShouldRejectClaim_WhenTaskStatusBlocksIt(
            ProjectTaskStatus taskStatus)
    {
        await using var context =
            _database.CreateDbContext();

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var scenario =
            await CreateScenarioAsync(
                context,
                UserRole.TenantAdmin,
                ProjectStatus.Planning,
                taskStatus);

        context.ChangeTracker.Clear();

        var result =
            await CreateHandler(context)
                .HandleAsync(
                    CreateCommand(scenario));

        Assert.Equal(
            ProjectTaskErrors.ClaimBlockedByTaskStatus,
            result.Error);

        context.ChangeTracker.Clear();

        var persistedTask =
            await GetTaskAsync(
                context,
                scenario.Task.PublicId);

        Assert.Equal(
            taskStatus,
            persistedTask.Status);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
        HandleAsync_ShouldRejectClaim_WhenTaskAlreadyHasResponsible()
    {
        await using var context =
            _database.CreateDbContext();

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var scenario =
            await CreateScenarioAsync(
                context,
                UserRole.TenantAdmin,
                ProjectStatus.Planning);

        scenario.Task.AssignResponsible(
            scenario.Requester.Id);

        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var result =
            await CreateHandler(context)
                .HandleAsync(
                    CreateCommand(scenario));

        Assert.Equal(
            ProjectTaskErrors.AlreadyAssigned,
            result.Error);

        context.ChangeTracker.Clear();

        var persistedTask =
            await GetTaskAsync(
                context,
                scenario.Task.PublicId);

        Assert.Equal(
            scenario.Requester.Id,
            persistedTask.ResponsibleUserId);

        Assert.Equal(
            ProjectTaskStatus.Backlog,
            persistedTask.Status);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
        HandleAsync_ShouldRejectTaskFromAnotherProject()
    {
        await using var context =
            _database.CreateDbContext();

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var scenario =
            await CreateScenarioAsync(
                context,
                UserRole.TenantAdmin,
                ProjectStatus.Planning);

        var otherProject =
            new Project(
                scenario.Tenant.Id,
                $"Outro projeto {Guid.NewGuid():N}",
                scenario.Requester.Id);

        context.Projects.Add(
            otherProject);

        await context.SaveChangesAsync();

        var otherTask =
            new ProjectTask(
                otherProject.Id,
                "Tarefa de outro projeto",
                ProjectTaskPriority.Medium,
                scenario.Requester.Id);

        context.ProjectTasks.Add(
            otherTask);

        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var command =
            CreateCommand(scenario) with
            {
                TaskPublicId =
                    otherTask.PublicId
            };

        var result =
            await CreateHandler(context)
                .HandleAsync(command);

        Assert.Equal(
            ProjectTaskErrors.NotFound,
            result.Error);

        context.ChangeTracker.Clear();

        var persistedTask =
            await GetTaskAsync(
                context,
                otherTask.PublicId);

        Assert.Null(
            persistedTask.ResponsibleUserId);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
        HandleAsync_ShouldNotAccessClaimantFromAnotherTenant()
    {
        await using var context =
            _database.CreateDbContext();

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var scenario =
            await CreateScenarioAsync(
                context,
                UserRole.TenantAdmin,
                ProjectStatus.Planning);

        var unique =
            Guid.NewGuid().ToString("N");

        var otherTenant =
            new Tenant(
                $"Outro Tenant {unique}",
                $"OTHER-{unique}",
                $"other-{unique}@test.local");

        context.Tenants.Add(
            otherTenant);

        await context.SaveChangesAsync();

        var otherUser =
            new User(
                otherTenant.Id,
                "Usuário de outro Tenant",
                $"foreign-{unique}@test.local",
                "password-hash",
                UserRole.Member);

        context.Users.Add(
            otherUser);

        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var command =
            CreateCommand(scenario) with
            {
                ClaimedByUserPublicId =
                    otherUser.PublicId
            };

        var result =
            await CreateHandler(context)
                .HandleAsync(command);

        Assert.Equal(
            UserErrors.NotFound,
            result.Error);

        context.ChangeTracker.Clear();

        var persistedTask =
            await GetTaskAsync(
                context,
                scenario.Task.PublicId);

        Assert.Null(
            persistedTask.ResponsibleUserId);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
        HandleAsync_ShouldAllowOnlyOneUserToClaimTask_WhenRequestsAreConcurrent()
    {
        var concurrentScenario =
            await CreateConcurrentScenarioAsync();

        try
        {
            await using var firstContext =
                _database.CreateDbContext();

            await using var secondContext =
                _database.CreateDbContext();

            var firstUnitOfWork =
                new CommitGateUnitOfWork(
                    firstContext);

            var firstHandler =
                CreateHandler(
                    firstContext,
                    firstUnitOfWork);

            var secondHandler =
                CreateHandler(
                    secondContext);

            var firstCommand =
                new ClaimProjectTaskCommand(
                    concurrentScenario.TenantPublicId,
                    concurrentScenario.ProjectPublicId,
                    concurrentScenario.TaskPublicId,
                    concurrentScenario.FirstUserPublicId);

            var secondCommand =
                new ClaimProjectTaskCommand(
                    concurrentScenario.TenantPublicId,
                    concurrentScenario.ProjectPublicId,
                    concurrentScenario.TaskPublicId,
                    concurrentScenario.SecondUserPublicId);

            var firstOperation =
                firstHandler.HandleAsync(
                    firstCommand);

            await firstUnitOfWork.WaitUntilCommitAsync();

            var secondOperation =
                secondHandler.HandleAsync(
                    secondCommand);

            await Task.Delay(
                TimeSpan.FromMilliseconds(250));

            Assert.False(
                secondOperation.IsCompleted);

            firstUnitOfWork.ReleaseCommit();

            var firstResult =
                await firstOperation;

            var secondResult =
                await secondOperation;

            Assert.True(
                firstResult.IsSuccess);

            Assert.Equal(
                concurrentScenario.FirstUserPublicId,
                firstResult.Value!.ResponsibleUserPublicId);

            Assert.True(
                secondResult.IsFailure);

            Assert.Equal(
                ProjectTaskErrors.AlreadyAssigned,
                secondResult.Error);

            await using var verificationContext =
                _database.CreateDbContext();

            var persistedTask =
                await verificationContext.ProjectTasks
                    .AsNoTracking()
                    .SingleAsync(
                        task =>
                            task.PublicId ==
                            concurrentScenario.TaskPublicId);

            var firstUser =
                await verificationContext.Users
                    .AsNoTracking()
                    .SingleAsync(
                        user =>
                            user.PublicId ==
                            concurrentScenario.FirstUserPublicId);

            Assert.Equal(
                firstUser.Id,
                persistedTask.ResponsibleUserId);

            Assert.Equal(
                ProjectTaskStatus.Backlog,
                persistedTask.Status);
        }
        finally
        {
            await CleanupConcurrentScenarioAsync(
                concurrentScenario.TenantPublicId);
        }
    }

    private static ClaimProjectTaskHandler CreateHandler(
        WorkFlowDbContext context,
        IUnitOfWork? unitOfWork = null)
    {
        return new ClaimProjectTaskHandler(
            new TenantRepository(context),
            new UserRepository(context),
            new ProjectRepository(context),
            new ProjectMemberRepository(context),
            new ProjectMemberPermissionRepository(context),
            new ProjectTaskRepository(context),
            unitOfWork ?? context);
    }

    private static ClaimProjectTaskCommand CreateCommand(
        Scenario scenario)
    {
        return new ClaimProjectTaskCommand(
            scenario.Tenant.PublicId,
            scenario.Project.PublicId,
            scenario.Task.PublicId,
            scenario.Requester.PublicId);
    }

    private static async Task<ProjectTask> GetTaskAsync(
        WorkFlowDbContext context,
        Guid taskPublicId)
    {
        return await context.ProjectTasks
            .AsNoTracking()
            .SingleAsync(
                task =>
                    task.PublicId ==
                    taskPublicId);
    }

    private static async Task<Scenario> CreateScenarioAsync(
        WorkFlowDbContext context,
        UserRole requesterRole,
        ProjectStatus projectStatus,
        ProjectTaskStatus taskStatus =
            ProjectTaskStatus.Backlog)
    {
        var unique =
            Guid.NewGuid().ToString("N");

        var tenant =
            new Tenant(
                $"Tenant Claim Task {unique}",
                $"REG-{unique}",
                $"tenant-{unique}@test.local");

        context.Tenants.Add(
            tenant);

        await context.SaveChangesAsync();

        var requester =
            new User(
                tenant.Id,
                "Usuário Claim",
                $"claim-{unique}@test.local",
                "password-hash",
                requesterRole);

        context.Users.Add(
            requester);

        await context.SaveChangesAsync();

        var project =
            new Project(
                tenant.Id,
                $"Projeto Claim Task {unique}",
                requester.Id);

        SetProjectStatus(
            project,
            projectStatus);

        context.Projects.Add(
            project);

        await context.SaveChangesAsync();

        var requesterMember =
            new ProjectMember(
                project.Id,
                requester.Id,
                requester.Id);

        context.ProjectMembers.Add(
            requesterMember);

        await context.SaveChangesAsync();

        ProjectMemberPermission? requesterPermission =
            null;

        if (requesterRole != UserRole.TenantAdmin)
        {
            requesterPermission =
                new ProjectMemberPermission(
                    requesterMember.Id,
                    ProjectPermission.ClaimTask,
                    requester.Id);

            context.ProjectMemberPermissions.Add(
                requesterPermission);

            await context.SaveChangesAsync();
        }

        var task =
            new ProjectTask(
                project.Id,
                "Tarefa disponível para claim",
                ProjectTaskPriority.High,
                requester.Id);

        SetTaskStatus(
            task,
            taskStatus,
            requester.Id);

        context.ProjectTasks.Add(
            task);

        await context.SaveChangesAsync();

        return new Scenario(
            tenant,
            requester,
            project,
            requesterMember,
            requesterPermission,
            task);
    }

    private async Task<ConcurrentScenario>
        CreateConcurrentScenarioAsync()
    {
        await using var context =
            _database.CreateDbContext();

        var unique =
            Guid.NewGuid().ToString("N");

        var tenant =
            new Tenant(
                $"Tenant Claim Concorrente {unique}",
                $"CONCURRENT-{unique}",
                $"concurrent-{unique}@test.local");

        context.Tenants.Add(
            tenant);

        await context.SaveChangesAsync();

        var firstUser =
            new User(
                tenant.Id,
                "Primeiro usuário",
                $"first-{unique}@test.local",
                "password-hash",
                UserRole.Member);

        var secondUser =
            new User(
                tenant.Id,
                "Segundo usuário",
                $"second-{unique}@test.local",
                "password-hash",
                UserRole.Member);

        context.Users.AddRange(
            firstUser,
            secondUser);

        await context.SaveChangesAsync();

        var project =
            new Project(
                tenant.Id,
                $"Projeto concorrente {unique}",
                firstUser.Id);

        context.Projects.Add(
            project);

        await context.SaveChangesAsync();

        var firstMember =
            new ProjectMember(
                project.Id,
                firstUser.Id,
                firstUser.Id);

        var secondMember =
            new ProjectMember(
                project.Id,
                secondUser.Id,
                firstUser.Id);

        context.ProjectMembers.AddRange(
            firstMember,
            secondMember);

        await context.SaveChangesAsync();

        var firstPermission =
            new ProjectMemberPermission(
                firstMember.Id,
                ProjectPermission.ClaimTask,
                firstUser.Id);

        var secondPermission =
            new ProjectMemberPermission(
                secondMember.Id,
                ProjectPermission.ClaimTask,
                firstUser.Id);

        context.ProjectMemberPermissions.AddRange(
            firstPermission,
            secondPermission);

        await context.SaveChangesAsync();

        var task =
            new ProjectTask(
                project.Id,
                "Tarefa concorrente",
                ProjectTaskPriority.High,
                firstUser.Id);

        context.ProjectTasks.Add(
            task);

        await context.SaveChangesAsync();

        return new ConcurrentScenario(
            tenant.PublicId,
            project.PublicId,
            task.PublicId,
            firstUser.PublicId,
            secondUser.PublicId);
    }

    private async Task CleanupConcurrentScenarioAsync(
        Guid tenantPublicId)
    {
        await using var context =
            _database.CreateDbContext();

        var tenant =
            await context.Tenants
                .SingleOrDefaultAsync(
                    tenant =>
                        tenant.PublicId ==
                        tenantPublicId);

        if (tenant is null)
            return;

        var projectIds =
            await context.Projects
                .Where(
                    project =>
                        project.TenantId ==
                        tenant.Id)
                .Select(
                    project =>
                        project.Id)
                .ToArrayAsync();

        var memberIds =
            await context.ProjectMembers
                .Where(
                    member =>
                        projectIds.Contains(
                            member.ProjectId))
                .Select(
                    member =>
                        member.Id)
                .ToArrayAsync();

        await context.ProjectMemberPermissions
            .Where(
                permission =>
                    memberIds.Contains(
                        permission.ProjectMemberId))
            .ExecuteDeleteAsync();

        await context.ProjectTasks
            .Where(
                task =>
                    projectIds.Contains(
                        task.ProjectId))
            .ExecuteDeleteAsync();

        await context.ProjectMembers
            .Where(
                member =>
                    projectIds.Contains(
                        member.ProjectId))
            .ExecuteDeleteAsync();

        await context.Projects
            .Where(
                project =>
                    project.TenantId ==
                    tenant.Id)
            .ExecuteDeleteAsync();

        await context.Users
            .Where(
                user =>
                    user.TenantId ==
                    tenant.Id)
            .ExecuteDeleteAsync();

        await context.Tenants
            .Where(
                currentTenant =>
                    currentTenant.Id ==
                    tenant.Id)
            .ExecuteDeleteAsync();
    }

    private static void SetProjectStatus(
        Project project,
        ProjectStatus status)
    {
        switch (status)
        {
            case ProjectStatus.Planning:
                return;

            case ProjectStatus.InProgress:
                project.Start();
                return;

            case ProjectStatus.Paused:
                project.Start();

                project.Pause(
                    "Projeto pausado para teste.");

                return;

            case ProjectStatus.Completed:
                project.Start();

                project.Complete(
                    new[]
                    {
                        ProjectTaskStatus.Done
                    });

                return;

            case ProjectStatus.Archived:
                project.Archive();
                return;

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(status));
        }
    }

    private static void SetTaskStatus(
        ProjectTask task,
        ProjectTaskStatus status,
        long requesterUserId)
    {
        switch (status)
        {
            case ProjectTaskStatus.Backlog:
                return;

            case ProjectTaskStatus.Todo:
                task.MoveToTodo();
                return;

            case ProjectTaskStatus.Paused:
                task.MoveToTodo();

                task.Pause(
                    "Tarefa pausada para teste.");

                return;

            case ProjectTaskStatus.InProgress:
                task.AssignResponsible(
                    requesterUserId);

                task.MoveToTodo();
                task.Start();

                return;

            case ProjectTaskStatus.Validation:
                task.AssignResponsible(
                    requesterUserId);

                task.MoveToTodo();
                task.Start();
                task.SendToValidation();

                return;

            case ProjectTaskStatus.Done:
                task.AssignResponsible(
                    requesterUserId);

                task.MoveToTodo();
                task.Start();
                task.SendToValidation();
                task.ClaimValidation(
                    requesterUserId);
                task.ApproveValidation(
                    requesterUserId);

                return;

            case ProjectTaskStatus.Cancelled:
                task.Cancel();
                return;

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(status));
        }
    }

    private sealed record Scenario(
        Tenant Tenant,
        User Requester,
        Project Project,
        ProjectMember RequesterMember,
        ProjectMemberPermission? RequesterPermission,
        ProjectTask Task);

    private sealed record ConcurrentScenario(
        Guid TenantPublicId,
        Guid ProjectPublicId,
        Guid TaskPublicId,
        Guid FirstUserPublicId,
        Guid SecondUserPublicId);

    private sealed class CommitGateUnitOfWork :
        IUnitOfWork
    {
        private readonly WorkFlowDbContext _context;

        private readonly TaskCompletionSource<bool>
            _commitReached =
                new(
                    TaskCreationOptions
                        .RunContinuationsAsynchronously);

        private readonly TaskCompletionSource<bool>
            _releaseCommit =
                new(
                    TaskCreationOptions
                        .RunContinuationsAsynchronously);

        public CommitGateUnitOfWork(
            WorkFlowDbContext context)
        {
            _context =
                context;
        }

        public async Task<IUnitOfWorkTransaction>
            BeginTransactionAsync(
                CancellationToken cancellationToken = default)
        {
            var transaction =
                await _context.BeginTransactionAsync(
                    cancellationToken);

            return new CommitGateTransaction(
                transaction,
                _commitReached,
                _releaseCommit);
        }

        public Task<int> SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
            return _context.SaveChangesAsync(
                cancellationToken);
        }

        public Task WaitUntilCommitAsync()
        {
            return _commitReached.Task;
        }

        public void ReleaseCommit()
        {
            _releaseCommit.TrySetResult(
                true);
        }

        private sealed class CommitGateTransaction :
            IUnitOfWorkTransaction
        {
            private readonly IUnitOfWorkTransaction
                _innerTransaction;

            private readonly TaskCompletionSource<bool>
                _commitReached;

            private readonly TaskCompletionSource<bool>
                _releaseCommit;

            public CommitGateTransaction(
                IUnitOfWorkTransaction innerTransaction,
                TaskCompletionSource<bool> commitReached,
                TaskCompletionSource<bool> releaseCommit)
            {
                _innerTransaction =
                    innerTransaction;

                _commitReached =
                    commitReached;

                _releaseCommit =
                    releaseCommit;
            }

            public async Task CommitAsync(
                CancellationToken cancellationToken = default)
            {
                _commitReached.TrySetResult(
                    true);

                await _releaseCommit.Task.WaitAsync(
                    cancellationToken);

                await _innerTransaction.CommitAsync(
                    cancellationToken);
            }

            public Task RollbackAsync(
                CancellationToken cancellationToken = default)
            {
                return _innerTransaction.RollbackAsync(
                    cancellationToken);
            }

            public ValueTask DisposeAsync()
            {
                return _innerTransaction.DisposeAsync();
            }
        }
    }
}