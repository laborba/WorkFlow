using Microsoft.EntityFrameworkCore;
using WorkFlow.Application.ProjectTasks;
using WorkFlow.Application.ProjectTasks.ClaimProjectTaskValidation;
using WorkFlow.Application.Tenants;
using WorkFlow.Application.Users;
using WorkFlow.Domain.Entities;
using WorkFlow.Domain.Enums;
using WorkFlow.Infrastructure.Persistence;
using WorkFlow.Infrastructure.Persistence.Repositories;
using WorkFlow.IntegrationTests.Infrastructure;

namespace WorkFlow.IntegrationTests.Application.ProjectTasks;

[Collection(TestCollectionNames.PostgreSql)]
public sealed class ClaimProjectTaskValidationIntegrationTests
{
    private static readonly DateTime DueDate =
        new(
            2027,
            10,
            31,
            18,
            0,
            0,
            DateTimeKind.Utc);

    private readonly PostgreSqlTestDatabase
        _database;

    public ClaimProjectTaskValidationIntegrationTests(
        PostgreSqlTestDatabase database)
    {
        _database =
            database;
    }

    [Fact]
    public async Task
        HandleAsync_ShouldPersistValidator_WhenRequesterIsTenantAdmin()
    {
        await using var context =
            _database.CreateDbContext();

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var scenario =
            await CreateScenarioAsync(
                context,
                UserRole.TenantAdmin);

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
            await GetTaskAsync(
                context,
                scenario.Task.PublicId);

        Assert.Equal(
            ProjectTaskStatus.Validation,
            persistedTask.Status);

        Assert.Equal(
            scenario.ResponsibleUser.Id,
            persistedTask.ResponsibleUserId);

        Assert.Equal(
            scenario.Requester.Id,
            persistedTask.ValidatorUserId);

        Assert.Equal(
            DueDate,
            persistedTask.DueDate);

        Assert.Null(
            persistedTask.StatusBeforePause);

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
            result.Value.ValidatorUserPublicId);

        Assert.Equal(
            ProjectTaskStatus.Validation,
            result.Value.Status);

        Assert.NotNull(
            result.Value.UpdatedAt);

        Assert.Equal(
            persistedTask.UpdatedAt!.Value,
            result.Value.UpdatedAt!.Value,
            TimeSpan.FromMicroseconds(1));

        await transaction.RollbackAsync();
    }

    [Theory]
    [InlineData(UserRole.ProjectManager)]
    [InlineData(UserRole.Member)]
    public async Task
        HandleAsync_ShouldPersistValidator_WhenRequesterHasValidateTaskPermission(
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
                permission:
                    ProjectPermission.ValidateTask);

        context.ChangeTracker.Clear();

        var result =
            await CreateHandler(context)
                .HandleAsync(
                    CreateCommand(scenario));

        Assert.True(
            result.IsSuccess);

        context.ChangeTracker.Clear();

        var persistedTask =
            await GetTaskAsync(
                context,
                scenario.Task.PublicId);

        Assert.Equal(
            ProjectTaskStatus.Validation,
            persistedTask.Status);

        Assert.Equal(
            scenario.ResponsibleUser.Id,
            persistedTask.ResponsibleUserId);

        Assert.Equal(
            scenario.Requester.Id,
            persistedTask.ValidatorUserId);

        await transaction.RollbackAsync();
    }

    [Theory]
    [InlineData(UserRole.TenantAdmin)]
    [InlineData(UserRole.ProjectManager)]
    [InlineData(UserRole.Member)]
    public async Task
        HandleAsync_ShouldRejectClaim_WhenRequesterMembershipWasRemoved(
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
                permission:
                    ProjectPermission.ValidateTask);

        scenario.RequesterMember.Remove();

        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var result =
            await CreateHandler(context)
                .HandleAsync(
                    CreateCommand(scenario));

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            ProjectTaskErrors.ClaimValidationNotAllowed,
            result.Error);

        context.ChangeTracker.Clear();

        var persistedTask =
            await GetTaskAsync(
                context,
                scenario.Task.PublicId);

        Assert.Null(
            persistedTask.ValidatorUserId);

        Assert.Equal(
            ProjectTaskStatus.Validation,
            persistedTask.Status);

        await transaction.RollbackAsync();
    }

    [Theory]
    [InlineData(ProjectStatus.Planning)]
    [InlineData(ProjectStatus.Paused)]
    [InlineData(ProjectStatus.Completed)]
    [InlineData(ProjectStatus.Archived)]
    public async Task
        HandleAsync_ShouldRejectClaim_WhenProjectIsNotInProgress(
            ProjectStatus projectStatus)
    {
        await using var context =
            _database.CreateDbContext();

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var scenario =
            await CreateScenarioAsync(
                context,
                projectStatus:
                    projectStatus,
                permission:
                    ProjectPermission.ValidateTask);

        context.ChangeTracker.Clear();

        var result =
            await CreateHandler(context)
                .HandleAsync(
                    CreateCommand(scenario));

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            ProjectTaskErrors
                .ClaimValidationBlockedByProjectStatus,
            result.Error);

        context.ChangeTracker.Clear();

        var persistedTask =
            await GetTaskAsync(
                context,
                scenario.Task.PublicId);

        Assert.Null(
            persistedTask.ValidatorUserId);

        Assert.Equal(
            ProjectTaskStatus.Validation,
            persistedTask.Status);

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
                taskStatus:
                    ProjectTaskStatus.Cancelled,
                permission:
                    ProjectPermission.ValidateTask);

        scenario.Task.Archive();

        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var result =
            await CreateHandler(context)
                .HandleAsync(
                    CreateCommand(scenario));

        Assert.True(
            result.IsFailure);

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
            persistedTask.ValidatorUserId);

        await transaction.RollbackAsync();
    }

    [Theory]
    [InlineData(ProjectTaskStatus.Backlog)]
    [InlineData(ProjectTaskStatus.Todo)]
    [InlineData(ProjectTaskStatus.InProgress)]
    [InlineData(ProjectTaskStatus.Paused)]
    [InlineData(ProjectTaskStatus.Done)]
    [InlineData(ProjectTaskStatus.Cancelled)]
    public async Task
        HandleAsync_ShouldRejectClaim_WhenTaskIsNotInValidation(
            ProjectTaskStatus taskStatus)
    {
        await using var context =
            _database.CreateDbContext();

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var scenario =
            await CreateScenarioAsync(
                context,
                taskStatus:
                    taskStatus,
                permission:
                    ProjectPermission.ValidateTask);

        context.ChangeTracker.Clear();

        var result =
            await CreateHandler(context)
                .HandleAsync(
                    CreateCommand(scenario));

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            ProjectTaskErrors
                .ClaimValidationBlockedByTaskStatus,
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
        HandleAsync_ShouldRejectClaim_WhenTaskHasNoResponsible()
    {
        await using var context =
            _database.CreateDbContext();

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var scenario =
            await CreateScenarioAsync(
                context,
                permission:
                    ProjectPermission.ValidateTask);

        context.Entry(
                scenario.Task)
            .Property(task =>
                task.ResponsibleUserId)
            .CurrentValue =
            null;

        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var result =
            await CreateHandler(context)
                .HandleAsync(
                    CreateCommand(scenario));

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            ProjectTaskErrors
                .ClaimValidationRequiresResponsible,
            result.Error);

        context.ChangeTracker.Clear();

        var persistedTask =
            await GetTaskAsync(
                context,
                scenario.Task.PublicId);

        Assert.Equal(
            ProjectTaskStatus.Validation,
            persistedTask.Status);

        Assert.Null(
            persistedTask.ResponsibleUserId);

        Assert.Null(
            persistedTask.ValidatorUserId);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
        HandleAsync_ShouldRejectClaim_WhenValidationAlreadyHasValidator()
    {
        await using var context =
            _database.CreateDbContext();

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var scenario =
            await CreateScenarioAsync(
                context,
                permission:
                    ProjectPermission.ValidateTask);

        scenario.Task.ClaimValidation(
            scenario.Requester.Id);

        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var result =
            await CreateHandler(context)
                .HandleAsync(
                    CreateCommand(scenario));

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            ProjectTaskErrors.ValidationAlreadyClaimed,
            result.Error);

        context.ChangeTracker.Clear();

        var persistedTask =
            await GetTaskAsync(
                context,
                scenario.Task.PublicId);

        Assert.Equal(
            scenario.Requester.Id,
            persistedTask.ValidatorUserId);

        Assert.Equal(
            ProjectTaskStatus.Validation,
            persistedTask.Status);

        await transaction.RollbackAsync();
    }

    [Theory]
    [InlineData(UserRole.ProjectManager)]
    [InlineData(UserRole.Member)]
    public async Task
        HandleAsync_ShouldAllowProjectResponsibleToClaimAnotherUsersTask(
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
                requesterIsProjectResponsible:
                    true);

        context.ChangeTracker.Clear();

        var result =
            await CreateHandler(context)
                .HandleAsync(
                    CreateCommand(scenario));

        Assert.True(
            result.IsSuccess);

        context.ChangeTracker.Clear();

        var persistedTask =
            await GetTaskAsync(
                context,
                scenario.Task.PublicId);

        Assert.Equal(
            scenario.ResponsibleUser.Id,
            persistedTask.ResponsibleUserId);

        Assert.Equal(
            scenario.Requester.Id,
            persistedTask.ValidatorUserId);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
        HandleAsync_ShouldAllowTenantAdminToSelfValidate()
    {
        await using var context =
            _database.CreateDbContext();

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var scenario =
            await CreateScenarioAsync(
                context,
                UserRole.TenantAdmin,
                selfValidation:
                    true);

        context.ChangeTracker.Clear();

        var result =
            await CreateHandler(context)
                .HandleAsync(
                    CreateCommand(scenario));

        Assert.True(
            result.IsSuccess);

        context.ChangeTracker.Clear();

        var persistedTask =
            await GetTaskAsync(
                context,
                scenario.Task.PublicId);

        Assert.Equal(
            scenario.Requester.Id,
            persistedTask.ResponsibleUserId);

        Assert.Equal(
            scenario.Requester.Id,
            persistedTask.ValidatorUserId);

        Assert.Equal(
            ProjectTaskStatus.Validation,
            persistedTask.Status);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
        HandleAsync_ShouldAllowProjectManagerResponsibleForProjectToSelfValidate()
    {
        await using var context =
            _database.CreateDbContext();

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var scenario =
            await CreateScenarioAsync(
                context,
                UserRole.ProjectManager,
                selfValidation:
                    true,
                requesterIsProjectResponsible:
                    true);

        context.ChangeTracker.Clear();

        var result =
            await CreateHandler(context)
                .HandleAsync(
                    CreateCommand(scenario));

        Assert.True(
            result.IsSuccess);

        context.ChangeTracker.Clear();

        var persistedTask =
            await GetTaskAsync(
                context,
                scenario.Task.PublicId);

        Assert.Equal(
            scenario.Requester.Id,
            persistedTask.ResponsibleUserId);

        Assert.Equal(
            scenario.Requester.Id,
            persistedTask.ValidatorUserId);

        await transaction.RollbackAsync();
    }

    [Theory]
    [InlineData(UserRole.ProjectManager)]
    [InlineData(UserRole.Member)]
    public async Task
        HandleAsync_ShouldAllowSelfValidation_WhenSelfValidateTaskPermissionExists(
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
                selfValidation:
                    true,
                permission:
                    ProjectPermission.SelfValidateTask);

        context.ChangeTracker.Clear();

        var result =
            await CreateHandler(context)
                .HandleAsync(
                    CreateCommand(scenario));

        Assert.True(
            result.IsSuccess);

        context.ChangeTracker.Clear();

        var persistedTask =
            await GetTaskAsync(
                context,
                scenario.Task.PublicId);

        Assert.Equal(
            scenario.Requester.Id,
            persistedTask.ResponsibleUserId);

        Assert.Equal(
            scenario.Requester.Id,
            persistedTask.ValidatorUserId);

        await transaction.RollbackAsync();
    }

    [Theory]
    [InlineData(UserRole.ProjectManager)]
    [InlineData(UserRole.Member)]
    public async Task
        HandleAsync_ShouldRejectSelfValidation_WhenRequesterOnlyHasValidateTask(
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
                selfValidation:
                    true,
                permission:
                    ProjectPermission.ValidateTask);

        context.ChangeTracker.Clear();

        var result =
            await CreateHandler(context)
                .HandleAsync(
                    CreateCommand(scenario));

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            ProjectTaskErrors.SelfValidationNotAllowed,
            result.Error);

        context.ChangeTracker.Clear();

        var persistedTask =
            await GetTaskAsync(
                context,
                scenario.Task.PublicId);

        Assert.Null(
            persistedTask.ValidatorUserId);

        Assert.Equal(
            scenario.Requester.Id,
            persistedTask.ResponsibleUserId);

        await transaction.RollbackAsync();
    }

    [Theory]
    [InlineData(UserRole.ProjectManager)]
    [InlineData(UserRole.Member)]
    public async Task
        HandleAsync_ShouldRejectOtherUsersTask_WhenRequesterOnlyHasSelfValidateTask(
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
                permission:
                    ProjectPermission.SelfValidateTask);

        context.ChangeTracker.Clear();

        var result =
            await CreateHandler(context)
                .HandleAsync(
                    CreateCommand(scenario));

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            ProjectTaskErrors.ClaimValidationNotAllowed,
            result.Error);

        context.ChangeTracker.Clear();

        var persistedTask =
            await GetTaskAsync(
                context,
                scenario.Task.PublicId);

        Assert.Null(
            persistedTask.ValidatorUserId);

        Assert.Equal(
            scenario.ResponsibleUser.Id,
            persistedTask.ResponsibleUserId);

        await transaction.RollbackAsync();
    }

    [Theory]
    [InlineData(UserRole.ProjectManager)]
    [InlineData(UserRole.Member)]
    public async Task
        HandleAsync_ShouldRejectClaim_WhenValidateTaskPermissionWasRevoked(
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
                permission:
                    ProjectPermission.ValidateTask);

        Assert.NotNull(
            scenario.Permission);

        scenario.Permission!.Revoke(
            scenario.Requester.Id);

        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var result =
            await CreateHandler(context)
                .HandleAsync(
                    CreateCommand(scenario));

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            ProjectTaskErrors.ClaimValidationNotAllowed,
            result.Error);

        context.ChangeTracker.Clear();

        var persistedTask =
            await GetTaskAsync(
                context,
                scenario.Task.PublicId);

        Assert.Null(
            persistedTask.ValidatorUserId);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
        HandleAsync_ShouldNotAccessRequesterFromAnotherTenant()
    {
        await using var context =
            _database.CreateDbContext();

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var scenario =
            await CreateScenarioAsync(
                context);

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
                .HandleAsync(
                    command);

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            UserErrors.NotFound,
            result.Error);

        context.ChangeTracker.Clear();

        var persistedTask =
            await GetTaskAsync(
                context,
                scenario.Task.PublicId);

        Assert.Null(
            persistedTask.ValidatorUserId);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
        HandleAsync_ShouldRejectClaim_WhenTenantIsInactive()
    {
        await using var context =
            _database.CreateDbContext();

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var scenario =
            await CreateScenarioAsync(
                context);

        scenario.Tenant.Deactivate();

        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var result =
            await CreateHandler(context)
                .HandleAsync(
                    CreateCommand(scenario));

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            TenantErrors.Inactive,
            result.Error);

        context.ChangeTracker.Clear();

        var persistedTask =
            await GetTaskAsync(
                context,
                scenario.Task.PublicId);

        Assert.Null(
            persistedTask.ValidatorUserId);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
        HandleAsync_ShouldRejectClaim_WhenRequesterIsInactive()
    {
        await using var context =
            _database.CreateDbContext();

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var scenario =
            await CreateScenarioAsync(
                context,
                permission:
                    ProjectPermission.ValidateTask);

        scenario.Requester.Deactivate();

        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var result =
            await CreateHandler(context)
                .HandleAsync(
                    CreateCommand(scenario));

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            UserErrors.Inactive,
            result.Error);

        context.ChangeTracker.Clear();

        var persistedTask =
            await GetTaskAsync(
                context,
                scenario.Task.PublicId);

        Assert.Null(
            persistedTask.ValidatorUserId);

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
                permission:
                    ProjectPermission.ValidateTask);

        var otherProject =
            new Project(
                scenario.Tenant.Id,
                $"Outro projeto {Guid.NewGuid():N}",
                scenario.Requester.Id);

        otherProject.Start();

        context.Projects.Add(
            otherProject);

        await context.SaveChangesAsync();

        var otherTask =
            new ProjectTask(
                otherProject.Id,
                "Tarefa de outro projeto",
                ProjectTaskPriority.Medium,
                scenario.Requester.Id,
                responsibleUserId:
                    scenario.ResponsibleUser.Id);

        otherTask.MoveToTodo();
        otherTask.Start();
        otherTask.SendToValidation();

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
                .HandleAsync(
                    command);

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            ProjectTaskErrors.NotFound,
            result.Error);

        context.ChangeTracker.Clear();

        var persistedTask =
            await GetTaskAsync(
                context,
                otherTask.PublicId);

        Assert.Equal(
            ProjectTaskStatus.Validation,
            persistedTask.Status);

        Assert.Null(
            persistedTask.ValidatorUserId);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
    HandleAsync_ShouldAllowOnlyOneValidator_WhenTwoUsersClaimConcurrently()
    {
        long tenantId = 0;

        try
        {
            Guid tenantPublicId;
            Guid projectPublicId;
            Guid taskPublicId;
            Guid firstValidatorPublicId;
            Guid secondValidatorPublicId;
            long firstValidatorId;
            long secondValidatorId;

            await using (
                var setupContext =
                    _database.CreateDbContext())
            {
                var scenario =
                    await CreateScenarioAsync(
                        setupContext,
                        UserRole.TenantAdmin);

                tenantId =
                    scenario.Tenant.Id;

                var secondValidator =
                    new User(
                        scenario.Tenant.Id,
                        "Segundo Validador",
                        $"second-validator-{Guid.NewGuid():N}@test.local",
                        "password-hash",
                        UserRole.TenantAdmin);

                setupContext.Users.Add(
                    secondValidator);

                await setupContext.SaveChangesAsync();

                var secondValidatorMember =
                    new ProjectMember(
                        scenario.Project.Id,
                        secondValidator.Id,
                        scenario.Requester.Id);

                setupContext.ProjectMembers.Add(
                    secondValidatorMember);

                await setupContext.SaveChangesAsync();

                tenantPublicId =
                    scenario.Tenant.PublicId;

                projectPublicId =
                    scenario.Project.PublicId;

                taskPublicId =
                    scenario.Task.PublicId;

                firstValidatorPublicId =
                    scenario.Requester.PublicId;

                secondValidatorPublicId =
                    secondValidator.PublicId;

                firstValidatorId =
                    scenario.Requester.Id;

                secondValidatorId =
                    secondValidator.Id;
            }

            await using var firstContext =
                _database.CreateDbContext();

            await using var secondContext =
                _database.CreateDbContext();

            var firstHandler =
                CreateHandler(
                    firstContext);

            var secondHandler =
                CreateHandler(
                    secondContext);

            var firstCommand =
                new ClaimProjectTaskValidationCommand(
                    tenantPublicId,
                    projectPublicId,
                    taskPublicId,
                    firstValidatorPublicId);

            var secondCommand =
                new ClaimProjectTaskValidationCommand(
                    tenantPublicId,
                    projectPublicId,
                    taskPublicId,
                    secondValidatorPublicId);

            var firstOperation =
                firstHandler.HandleAsync(
                    firstCommand);

            var secondOperation =
                secondHandler.HandleAsync(
                    secondCommand);

            var results =
                await Task.WhenAll(
                    firstOperation,
                    secondOperation);

            var successfulResults =
                results
                    .Where(result =>
                        result.IsSuccess)
                    .ToList();

            var failedResults =
                results
                    .Where(result =>
                        result.IsFailure)
                    .ToList();

            var success =
                Assert.Single(
                    successfulResults);

            var failure =
                Assert.Single(
                    failedResults);

            Assert.Equal(
                ProjectTaskErrors.ValidationAlreadyClaimed,
                failure.Error);

            Assert.NotNull(
                success.Value);

            await using var verificationContext =
                _database.CreateDbContext();

            var persistedTask =
                await GetTaskAsync(
                    verificationContext,
                    taskPublicId);

            Assert.Equal(
                ProjectTaskStatus.Validation,
                persistedTask.Status);

            Assert.NotNull(
                persistedTask.ValidatorUserId);

            Assert.True(
                persistedTask.ValidatorUserId ==
                    firstValidatorId ||
                persistedTask.ValidatorUserId ==
                    secondValidatorId);

            var expectedValidatorPublicId =
                persistedTask.ValidatorUserId ==
                    firstValidatorId
                    ? firstValidatorPublicId
                    : secondValidatorPublicId;

            Assert.Equal(
                expectedValidatorPublicId,
                success.Value!.ValidatorUserPublicId);
        }
        finally
        {
            if (tenantId > 0)
            {
                await CleanupTenantAsync(
                    tenantId);
            }
        }
    }

    private static ClaimProjectTaskValidationHandler CreateHandler(
        WorkFlowDbContext context)
    {
        return new ClaimProjectTaskValidationHandler(
            new TenantRepository(context),
            new UserRepository(context),
            new ProjectRepository(context),
            new ProjectMemberRepository(context),
            new ProjectMemberPermissionRepository(context),
            new ProjectTaskRepository(context),
            context);
    }

    private static ClaimProjectTaskValidationCommand CreateCommand(
        Scenario scenario)
    {
        return new ClaimProjectTaskValidationCommand(
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
        UserRole requesterRole = UserRole.Member,
        ProjectStatus projectStatus =
            ProjectStatus.InProgress,
        ProjectTaskStatus taskStatus =
            ProjectTaskStatus.Validation,
        bool selfValidation = false,
        bool requesterIsProjectResponsible = false,
        ProjectPermission? permission = null)
    {
        var unique =
            Guid.NewGuid().ToString("N");

        var tenant =
            new Tenant(
                $"Tenant Claim Validation {unique}",
                $"REG-{unique}",
                $"tenant-{unique}@test.local");

        context.Tenants.Add(
            tenant);

        await context.SaveChangesAsync();

        var requester =
            new User(
                tenant.Id,
                "Usuário Validador",
                $"validator-{unique}@test.local",
                "password-hash",
                requesterRole);

        context.Users.Add(
            requester);

        await context.SaveChangesAsync();

        User responsibleUser;

        if (selfValidation)
        {
            responsibleUser =
                requester;
        }
        else
        {
            responsibleUser =
                new User(
                    tenant.Id,
                    "Usuário Responsável",
                    $"responsible-{unique}@test.local",
                    "password-hash",
                    UserRole.Member);

            context.Users.Add(
                responsibleUser);

            await context.SaveChangesAsync();
        }

        var project =
            new Project(
                tenant.Id,
                $"Projeto Claim Validation {unique}",
                requester.Id,
                responsibleUserId:
                    requesterIsProjectResponsible
                        ? requester.Id
                        : null);

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

        ProjectMember? responsibleMember =
            null;

        if (responsibleUser.Id != requester.Id)
        {
            responsibleMember =
                new ProjectMember(
                    project.Id,
                    responsibleUser.Id,
                    requester.Id);

            context.ProjectMembers.Add(
                responsibleMember);
        }

        await context.SaveChangesAsync();

        ProjectMemberPermission? projectPermission =
            null;

        if (permission.HasValue)
        {
            projectPermission =
                new ProjectMemberPermission(
                    requesterMember.Id,
                    permission.Value,
                    requester.Id);

            context.ProjectMemberPermissions.Add(
                projectPermission);

            await context.SaveChangesAsync();
        }

        var task =
            new ProjectTask(
                project.Id,
                "Tarefa aguardando validação",
                ProjectTaskPriority.High,
                requester.Id,
                responsibleUserId:
                    responsibleUser.Id,
                dueDate:
                    DueDate);

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
            responsibleUser,
            project,
            requesterMember,
            responsibleMember,
            projectPermission,
            task);
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

            case ProjectTaskStatus.InProgress:
                task.MoveToTodo();
                task.Start();
                return;

            case ProjectTaskStatus.Paused:
                task.MoveToTodo();
                task.Start();

                task.Pause(
                    "Tarefa pausada para teste.");

                return;

            case ProjectTaskStatus.Validation:
                task.MoveToTodo();
                task.Start();
                task.SendToValidation();
                return;

            case ProjectTaskStatus.Done:
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

    private async Task CleanupTenantAsync(
        long tenantId)
    {
        await using var context =
            _database.CreateDbContext();

        var projectIds =
            await context.Projects
                .Where(project =>
                    project.TenantId == tenantId)
                .Select(project =>
                    project.Id)
                .ToListAsync();

        if (projectIds.Count > 0)
        {
            var projectMemberIds =
                await context.ProjectMembers
                    .Where(member =>
                        projectIds.Contains(
                            member.ProjectId))
                    .Select(member =>
                        member.Id)
                    .ToListAsync();

            if (projectMemberIds.Count > 0)
            {
                await context.ProjectMemberPermissions
                    .Where(permission =>
                        projectMemberIds.Contains(
                            permission.ProjectMemberId))
                    .ExecuteDeleteAsync();
            }

            await context.ProjectTasks
                .Where(task =>
                    projectIds.Contains(
                        task.ProjectId))
                .ExecuteDeleteAsync();

            await context.ProjectMembers
                .Where(member =>
                    projectIds.Contains(
                        member.ProjectId))
                .ExecuteDeleteAsync();

            await context.Projects
                .Where(project =>
                    project.TenantId == tenantId)
                .ExecuteDeleteAsync();
        }

        await context.Users
            .Where(user =>
                user.TenantId == tenantId)
            .ExecuteDeleteAsync();

        await context.Tenants
            .Where(tenant =>
                tenant.Id == tenantId)
            .ExecuteDeleteAsync();
    }

    private sealed record Scenario(
        Tenant Tenant,
        User Requester,
        User ResponsibleUser,
        Project Project,
        ProjectMember RequesterMember,
        ProjectMember? ResponsibleMember,
        ProjectMemberPermission? Permission,
        ProjectTask Task);
}