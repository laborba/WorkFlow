using System.Reflection;
using WorkFlow.Application.Common.Errors;
using WorkFlow.Application.Projects;
using WorkFlow.Application.ProjectTasks;
using WorkFlow.Application.ProjectTasks.ClaimProjectTaskValidation;
using WorkFlow.Application.Tenants;
using WorkFlow.Application.Users;
using WorkFlow.Domain.Entities;
using WorkFlow.Domain.Enums;
using WorkFlow.UnitTests.Application.Projects.Fakes;
using WorkFlow.UnitTests.Application.Tenants.Fakes;
using WorkFlow.UnitTests.Application.Users.Fakes;
using WorkFlow.UnitTests.Common;
using WorkFlow.UnitTests.Common.Fakes;

namespace WorkFlow.UnitTests.Application.ProjectTasks.ClaimProjectTaskValidation;

public sealed class ClaimProjectTaskValidationHandlerTests
{
    [Fact]
    public async Task HandleAsync_ShouldThrow_WhenCommandIsNull()
    {
        var fixture =
            CreateFixture();

        await Assert.ThrowsAsync<ArgumentNullException>(
            () =>
                fixture.Handler.HandleAsync(
                    null!));

        Assert.Equal(
            0,
            fixture.UnitOfWork.BeginTransactionCallCount);

        AssertNothingPersisted(
            fixture);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public async Task HandleAsync_ShouldThrow_WhenRequiredPublicIdIsEmpty(
        int publicIdToClear)
    {
        var fixture =
            CreateFixture();

        var command =
            fixture.CreateCommand();

        command =
            publicIdToClear switch
            {
                1 => command with
                {
                    TenantPublicId = Guid.Empty
                },

                2 => command with
                {
                    ProjectPublicId = Guid.Empty
                },

                3 => command with
                {
                    TaskPublicId = Guid.Empty
                },

                _ => command with
                {
                    ClaimedByUserPublicId = Guid.Empty
                }
            };

        await Assert.ThrowsAsync<ArgumentException>(
            () =>
                fixture.Handler.HandleAsync(
                    command));

        Assert.Equal(
            0,
            fixture.UnitOfWork.BeginTransactionCallCount);

        AssertNothingPersisted(
            fixture);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnNotFound_WhenTenantDoesNotExist()
    {
        var fixture =
            CreateFixture();

        fixture.TenantRepository.TenantToReturn =
            null;

        await AssertFailureAsync(
            fixture,
            TenantErrors.NotFound);

        Assert.Equal(
            0,
            fixture.UnitOfWork.BeginTransactionCallCount);
    }

    [Fact]
    public async Task HandleAsync_ShouldRejectInactiveTenant()
    {
        var fixture =
            CreateFixture();

        fixture.Tenant.Deactivate();

        await AssertFailureAsync(
            fixture,
            TenantErrors.Inactive);

        Assert.Equal(
            0,
            fixture.UnitOfWork.BeginTransactionCallCount);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnNotFound_WhenRequesterDoesNotExist()
    {
        var fixture =
            CreateFixture();

        fixture.UserRepository
            .UsersByPublicIdToReturn
            .Remove(
                fixture.Requester.PublicId);

        await AssertFailureAsync(
            fixture,
            UserErrors.NotFound);

        Assert.Equal(
            0,
            fixture.UnitOfWork.BeginTransactionCallCount);
    }

    [Fact]
    public async Task HandleAsync_ShouldRejectInactiveRequester()
    {
        var fixture =
            CreateFixture();

        fixture.Requester.Deactivate();

        await AssertFailureAsync(
            fixture,
            UserErrors.Inactive);

        Assert.Equal(
            0,
            fixture.UnitOfWork.BeginTransactionCallCount);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnNotFound_WhenProjectDoesNotExist()
    {
        var fixture =
            CreateFixture();

        fixture.ProjectRepository.ProjectToReturn =
            null;

        await AssertFailureAsync(
            fixture,
            ProjectErrors.NotFound);

        AssertTransactionalFailure(
            fixture);
    }

    [Theory]
    [InlineData(ProjectStatus.Planning)]
    [InlineData(ProjectStatus.Paused)]
    [InlineData(ProjectStatus.Completed)]
    [InlineData(ProjectStatus.Archived)]
    public async Task HandleAsync_ShouldRejectClaim_WhenProjectIsNotInProgress(
        ProjectStatus projectStatus)
    {
        var fixture =
            CreateFixture(
                projectStatus:
                    projectStatus);

        await AssertFailureAsync(
            fixture,
            ProjectTaskErrors
                .ClaimValidationBlockedByProjectStatus);

        AssertTransactionalFailure(
            fixture);

        Assert.Empty(
            fixture.TaskRepository.GetForUpdateCalls);
    }

    [Theory]
    [InlineData(UserRole.TenantAdmin)]
    [InlineData(UserRole.ProjectManager)]
    [InlineData(UserRole.Member)]
    public async Task HandleAsync_ShouldRejectRequester_WhenMembershipIsNotActive(
        UserRole role)
    {
        var fixture =
            CreateFixture(
                role);

        fixture.MemberRepository
            .ActiveMembersToReturn[
                (
                    fixture.Project.Id,
                    fixture.Requester.Id
                )] =
            null;

        await AssertFailureAsync(
            fixture,
            ProjectTaskErrors.ClaimValidationNotAllowed);

        AssertTransactionalFailure(
            fixture);

        Assert.Empty(
            fixture.PermissionRepository
                .IsActivePermissionCalls);

        Assert.Empty(
            fixture.TaskRepository
                .GetForUpdateCalls);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnNotFound_WhenTaskDoesNotExist()
    {
        var fixture =
            CreateFixture();

        fixture.TaskRepository
            .ProjectTaskForUpdateToReturn =
            null;

        await AssertFailureAsync(
            fixture,
            ProjectTaskErrors.NotFound);

        AssertTransactionalFailure(
            fixture);
    }

    [Fact]
    public async Task HandleAsync_ShouldRejectClaim_WhenTaskIsArchived()
    {
        var fixture =
            CreateFixture();

        var task =
            new ProjectTask(
                fixture.Project.Id,
                "Tarefa arquivada",
                ProjectTaskPriority.Medium,
                fixture.Requester.Id,
                responsibleUserId:
                    fixture.ResponsibleUser.Id);

        EntityTestHelper.SetId(
            task,
            201);

        task.Cancel();
        task.Archive();

        fixture.TaskRepository
            .ProjectTaskForUpdateToReturn =
            task;

        await AssertFailureAsync(
            fixture,
            ProjectTaskErrors.Archived,
            fixture.CreateCommand(
                task.PublicId));

        Assert.True(
            task.IsArchived);

        AssertTransactionalFailure(
            fixture);
    }

    [Theory]
    [InlineData(ProjectTaskStatus.Backlog)]
    [InlineData(ProjectTaskStatus.Todo)]
    [InlineData(ProjectTaskStatus.InProgress)]
    [InlineData(ProjectTaskStatus.Paused)]
    [InlineData(ProjectTaskStatus.Done)]
    [InlineData(ProjectTaskStatus.Cancelled)]
    public async Task HandleAsync_ShouldRejectClaim_WhenTaskIsNotInValidation(
        ProjectTaskStatus taskStatus)
    {
        var fixture =
            CreateFixture();

        var task =
            CreateTaskWithStatus(
                fixture,
                taskStatus);

        fixture.TaskRepository
            .ProjectTaskForUpdateToReturn =
            task;

        await AssertFailureAsync(
            fixture,
            ProjectTaskErrors
                .ClaimValidationBlockedByTaskStatus,
            fixture.CreateCommand(
                task.PublicId));

        Assert.Equal(
            taskStatus,
            task.Status);

        AssertTransactionalFailure(
            fixture);
    }

    [Fact]
    public async Task HandleAsync_ShouldRejectClaim_WhenTaskHasNoResponsible()
    {
        var fixture =
            CreateFixture();

        SetResponsibleUserId(
            fixture.ProjectTask,
            null);

        await AssertFailureAsync(
            fixture,
            ProjectTaskErrors
                .ClaimValidationRequiresResponsible);

        Assert.Equal(
            ProjectTaskStatus.Validation,
            fixture.ProjectTask.Status);

        Assert.Null(
            fixture.ProjectTask.ResponsibleUserId);

        AssertTransactionalFailure(
            fixture);
    }

    [Fact]
    public async Task HandleAsync_ShouldRejectClaim_WhenValidationAlreadyHasValidator()
    {
        var fixture =
            CreateFixture();

        fixture.ProjectTask.ClaimValidation(
            fixture.Requester.Id);

        var validatorBefore =
            fixture.ProjectTask.ValidatorUserId;

        await AssertFailureAsync(
            fixture,
            ProjectTaskErrors.ValidationAlreadyClaimed);

        Assert.Equal(
            validatorBefore,
            fixture.ProjectTask.ValidatorUserId);

        AssertTransactionalFailure(
            fixture);

        Assert.Empty(
            fixture.PermissionRepository
                .IsActivePermissionCalls);
    }

    [Fact]
    public async Task HandleAsync_ShouldClaimValidation_WhenRequesterIsTenantAdmin()
    {
        var fixture =
            CreateFixture(
                UserRole.TenantAdmin);

        var responsibleBefore =
            fixture.ProjectTask.ResponsibleUserId;

        var dueDateBefore =
            fixture.ProjectTask.DueDate;

        var statusBeforePause =
            fixture.ProjectTask.StatusBeforePause;

        var before =
            DateTime.UtcNow;

        var result =
            await fixture.Handler.HandleAsync(
                fixture.CreateCommand());

        Assert.True(
            result.IsSuccess);

        Assert.False(
            result.IsFailure);

        Assert.Null(
            result.Error);

        var response =
            Assert.IsType<ClaimProjectTaskValidationResult>(
                result.Value);

        Assert.Equal(
            ProjectTaskStatus.Validation,
            fixture.ProjectTask.Status);

        Assert.Equal(
            fixture.Requester.Id,
            fixture.ProjectTask.ValidatorUserId);

        Assert.Equal(
            responsibleBefore,
            fixture.ProjectTask.ResponsibleUserId);

        Assert.Equal(
            dueDateBefore,
            fixture.ProjectTask.DueDate);

        Assert.Equal(
            statusBeforePause,
            fixture.ProjectTask.StatusBeforePause);

        Assert.NotNull(
            fixture.ProjectTask.UpdatedAt);

        Assert.InRange(
            fixture.ProjectTask.UpdatedAt!.Value,
            before,
            DateTime.UtcNow);

        Assert.Equal(
            fixture.ProjectTask.PublicId,
            response.PublicId);

        Assert.Equal(
            fixture.Tenant.PublicId,
            response.TenantPublicId);

        Assert.Equal(
            fixture.Project.PublicId,
            response.ProjectPublicId);

        Assert.Equal(
            fixture.Requester.PublicId,
            response.ValidatorUserPublicId);

        Assert.Equal(
            ProjectTaskStatus.Validation,
            response.Status);

        Assert.Equal(
            fixture.ProjectTask.UpdatedAt,
            response.UpdatedAt);

        Assert.Equal(
            1,
            fixture.UnitOfWork.BeginTransactionCallCount);

        Assert.Equal(
            1,
            fixture.UnitOfWork.SaveChangesCallCount);

        Assert.Equal(
            1,
            fixture.UnitOfWork.CommitCallCount);

        Assert.Equal(
            0,
            fixture.UnitOfWork.RollbackCallCount);

        Assert.Empty(
            fixture.PermissionRepository
                .IsActivePermissionCalls);

        var membershipCall =
            Assert.Single(
                fixture.MemberRepository.GetActiveCalls);

        Assert.Equal(
            (
                fixture.Project.Id,
                fixture.Requester.Id
            ),
            membershipCall);
    }

    [Fact]
    public async Task HandleAsync_ShouldAllowTenantAdminToSelfValidate()
    {
        var fixture =
            CreateFixture(
                UserRole.TenantAdmin);

        var task =
            CreateValidationTask(
                fixture,
                fixture.Requester.Id,
                201);

        fixture.TaskRepository
            .ProjectTaskForUpdateToReturn =
            task;

        fixture.PermissionRepository
            .IsActivePermissionResults[
                (
                    fixture.RequesterMembership.Id,
                    ProjectPermission.SelfValidateTask
                )] =
            false;

        var result =
            await fixture.Handler.HandleAsync(
                fixture.CreateCommand(
                    task.PublicId));

        Assert.True(
            result.IsSuccess);

        Assert.Equal(
            fixture.Requester.Id,
            task.ResponsibleUserId);

        Assert.Equal(
            fixture.Requester.Id,
            task.ValidatorUserId);

        Assert.Equal(
            ProjectTaskStatus.Validation,
            task.Status);

        Assert.Empty(
            fixture.PermissionRepository
                .IsActivePermissionCalls);
    }

    [Theory]
    [InlineData(UserRole.ProjectManager)]
    [InlineData(UserRole.Member)]
    public async Task HandleAsync_ShouldAllowProjectResponsibleToClaimValidationFromAnotherUser(
        UserRole role)
    {
        var fixture =
            CreateFixture(
                role);

        fixture.Project.AssignResponsible(
            fixture.Requester.Id);

        fixture.PermissionRepository
            .IsActivePermissionResults[
                (
                    fixture.RequesterMembership.Id,
                    ProjectPermission.ValidateTask
                )] =
            false;

        var result =
            await fixture.Handler.HandleAsync(
                fixture.CreateCommand());

        Assert.True(
            result.IsSuccess);

        Assert.Equal(
            fixture.Requester.Id,
            fixture.ProjectTask.ValidatorUserId);

        Assert.Equal(
            fixture.ResponsibleUser.Id,
            fixture.ProjectTask.ResponsibleUserId);

        Assert.Empty(
            fixture.PermissionRepository
                .IsActivePermissionCalls);
    }

    [Theory]
    [InlineData(UserRole.ProjectManager)]
    [InlineData(UserRole.Member)]
    public async Task HandleAsync_ShouldClaimValidation_WhenRequesterHasValidateTaskPermission(
        UserRole role)
    {
        var fixture =
            CreateFixture(
                role);

        fixture.PermissionRepository
            .IsActivePermissionResults[
                (
                    fixture.RequesterMembership.Id,
                    ProjectPermission.ValidateTask
                )] =
            true;

        var result =
            await fixture.Handler.HandleAsync(
                fixture.CreateCommand());

        Assert.True(
            result.IsSuccess);

        Assert.Equal(
            fixture.Requester.Id,
            fixture.ProjectTask.ValidatorUserId);

        Assert.Equal(
            (
                fixture.RequesterMembership.Id,
                ProjectPermission.ValidateTask
            ),
            Assert.Single(
                fixture.PermissionRepository
                    .IsActivePermissionCalls));

        Assert.Equal(
            1,
            fixture.UnitOfWork.SaveChangesCallCount);

        Assert.Equal(
            1,
            fixture.UnitOfWork.CommitCallCount);

        Assert.Equal(
            0,
            fixture.UnitOfWork.RollbackCallCount);
    }

    [Theory]
    [InlineData(UserRole.ProjectManager)]
    [InlineData(UserRole.Member)]
    public async Task HandleAsync_ShouldRejectClaim_WhenValidateTaskPermissionIsMissing(
        UserRole role)
    {
        var fixture =
            CreateFixture(
                role);

        fixture.PermissionRepository
            .IsActivePermissionResults[
                (
                    fixture.RequesterMembership.Id,
                    ProjectPermission.ValidateTask
                )] =
            false;

        await AssertFailureAsync(
            fixture,
            ProjectTaskErrors.ClaimValidationNotAllowed);

        Assert.Equal(
            (
                fixture.RequesterMembership.Id,
                ProjectPermission.ValidateTask
            ),
            Assert.Single(
                fixture.PermissionRepository
                    .IsActivePermissionCalls));

        Assert.Null(
            fixture.ProjectTask.ValidatorUserId);

        AssertTransactionalFailure(
            fixture);
    }

    [Fact]
    public async Task HandleAsync_ShouldAllowProjectManagerResponsibleForProjectToSelfValidate()
    {
        var fixture =
            CreateFixture(
                UserRole.ProjectManager);

        fixture.Project.AssignResponsible(
            fixture.Requester.Id);

        var task =
            CreateValidationTask(
                fixture,
                fixture.Requester.Id,
                201);

        fixture.TaskRepository
            .ProjectTaskForUpdateToReturn =
            task;

        fixture.PermissionRepository
            .IsActivePermissionResults[
                (
                    fixture.RequesterMembership.Id,
                    ProjectPermission.SelfValidateTask
                )] =
            false;

        var result =
            await fixture.Handler.HandleAsync(
                fixture.CreateCommand(
                    task.PublicId));

        Assert.True(
            result.IsSuccess);

        Assert.Equal(
            fixture.Requester.Id,
            task.ResponsibleUserId);

        Assert.Equal(
            fixture.Requester.Id,
            task.ValidatorUserId);

        Assert.Empty(
            fixture.PermissionRepository
                .IsActivePermissionCalls);
    }

    [Theory]
    [InlineData(UserRole.ProjectManager)]
    [InlineData(UserRole.Member)]
    public async Task HandleAsync_ShouldAllowSelfValidation_WhenRequesterHasSelfValidateTaskPermission(
        UserRole role)
    {
        var fixture =
            CreateFixture(
                role);

        var task =
            CreateValidationTask(
                fixture,
                fixture.Requester.Id,
                201);

        fixture.TaskRepository
            .ProjectTaskForUpdateToReturn =
            task;

        fixture.PermissionRepository
            .IsActivePermissionResults[
                (
                    fixture.RequesterMembership.Id,
                    ProjectPermission.ValidateTask
                )] =
            false;

        fixture.PermissionRepository
            .IsActivePermissionResults[
                (
                    fixture.RequesterMembership.Id,
                    ProjectPermission.SelfValidateTask
                )] =
            true;

        var result =
            await fixture.Handler.HandleAsync(
                fixture.CreateCommand(
                    task.PublicId));

        Assert.True(
            result.IsSuccess);

        Assert.Equal(
            fixture.Requester.Id,
            task.ValidatorUserId);

        Assert.Equal(
            (
                fixture.RequesterMembership.Id,
                ProjectPermission.SelfValidateTask
            ),
            Assert.Single(
                fixture.PermissionRepository
                    .IsActivePermissionCalls));
    }

    [Theory]
    [InlineData(UserRole.ProjectManager)]
    [InlineData(UserRole.Member)]
    public async Task HandleAsync_ShouldRejectSelfValidation_WhenRequesterOnlyHasValidateTaskPermission(
        UserRole role)
    {
        var fixture =
            CreateFixture(
                role);

        var task =
            CreateValidationTask(
                fixture,
                fixture.Requester.Id,
                201);

        fixture.TaskRepository
            .ProjectTaskForUpdateToReturn =
            task;

        fixture.PermissionRepository
            .IsActivePermissionResults[
                (
                    fixture.RequesterMembership.Id,
                    ProjectPermission.ValidateTask
                )] =
            true;

        fixture.PermissionRepository
            .IsActivePermissionResults[
                (
                    fixture.RequesterMembership.Id,
                    ProjectPermission.SelfValidateTask
                )] =
            false;

        await AssertFailureAsync(
            fixture,
            ProjectTaskErrors.SelfValidationNotAllowed,
            fixture.CreateCommand(
                task.PublicId));

        Assert.Null(
            task.ValidatorUserId);

        Assert.Equal(
            (
                fixture.RequesterMembership.Id,
                ProjectPermission.SelfValidateTask
            ),
            Assert.Single(
                fixture.PermissionRepository
                    .IsActivePermissionCalls));

        AssertTransactionalFailure(
            fixture);
    }

    [Theory]
    [InlineData(UserRole.ProjectManager)]
    [InlineData(UserRole.Member)]
    public async Task HandleAsync_ShouldRejectClaimForAnotherUser_WhenRequesterOnlyHasSelfValidateTaskPermission(
        UserRole role)
    {
        var fixture =
            CreateFixture(
                role);

        fixture.PermissionRepository
            .IsActivePermissionResults[
                (
                    fixture.RequesterMembership.Id,
                    ProjectPermission.ValidateTask
                )] =
            false;

        fixture.PermissionRepository
            .IsActivePermissionResults[
                (
                    fixture.RequesterMembership.Id,
                    ProjectPermission.SelfValidateTask
                )] =
            true;

        await AssertFailureAsync(
            fixture,
            ProjectTaskErrors.ClaimValidationNotAllowed);

        Assert.Null(
            fixture.ProjectTask.ValidatorUserId);

        Assert.Equal(
            (
                fixture.RequesterMembership.Id,
                ProjectPermission.ValidateTask
            ),
            Assert.Single(
                fixture.PermissionRepository
                    .IsActivePermissionCalls));

        AssertTransactionalFailure(
            fixture);
    }

    [Fact]
    public async Task HandleAsync_ShouldRollback_WhenSaveChangesFails()
    {
        var fixture =
            CreateFixture();

        fixture.UnitOfWork.OnSaveChanges =
            _ =>
                throw new InvalidOperationException(
                    "Falha simulada ao assumir validação.");

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () =>
                    fixture.Handler.HandleAsync(
                        fixture.CreateCommand()));

        Assert.Equal(
            "Falha simulada ao assumir validação.",
            exception.Message);

        Assert.Equal(
            1,
            fixture.UnitOfWork.BeginTransactionCallCount);

        Assert.Equal(
            1,
            fixture.UnitOfWork.SaveChangesCallCount);

        Assert.Equal(
            0,
            fixture.UnitOfWork.CommitCallCount);

        Assert.Equal(
            1,
            fixture.UnitOfWork.RollbackCallCount);
    }

    private static Fixture CreateFixture(
        UserRole requesterRole = UserRole.Member,
        ProjectStatus projectStatus = ProjectStatus.InProgress)
    {
        var tenant =
            new Tenant(
                "Empresa Claim Validation",
                $"REG-{Guid.NewGuid():N}",
                $"tenant-{Guid.NewGuid():N}@test.local");

        EntityTestHelper.SetId(
            tenant,
            42);

        var requester =
            new User(
                tenant.Id,
                "Usuário Validador",
                $"validator-{Guid.NewGuid():N}@test.local",
                "password-hash",
                requesterRole);

        EntityTestHelper.SetId(
            requester,
            10);

        var responsibleUser =
            new User(
                tenant.Id,
                "Usuário Responsável",
                $"responsible-{Guid.NewGuid():N}@test.local",
                "password-hash",
                UserRole.Member);

        EntityTestHelper.SetId(
            responsibleUser,
            20);

        var project =
            new Project(
                tenant.Id,
                "Projeto Claim Validation",
                requester.Id);

        EntityTestHelper.SetId(
            project,
            100);

        SetProjectStatus(
            project,
            projectStatus);

        var dueDate =
            new DateTime(
                2027,
                8,
                31,
                18,
                0,
                0,
                DateTimeKind.Utc);

        var projectTask =
            new ProjectTask(
                project.Id,
                "Tarefa aguardando validação",
                ProjectTaskPriority.Medium,
                requester.Id,
                responsibleUserId:
                    responsibleUser.Id,
                dueDate:
                    dueDate);

        EntityTestHelper.SetId(
            projectTask,
            200);

        projectTask.MoveToTodo();
        projectTask.Start();
        projectTask.SendToValidation();

        var requesterMembership =
            new ProjectMember(
                project.Id,
                requester.Id,
                requester.Id);

        EntityTestHelper.SetId(
            requesterMembership,
            300);

        var tenantRepository =
            new FakeTenantRepository
            {
                TenantToReturn =
                    tenant
            };

        var userRepository =
            new FakeUserRepository();

        userRepository
            .UsersByPublicIdToReturn[
                requester.PublicId] =
            requester;

        var projectRepository =
            new FakeProjectRepository
            {
                ProjectToReturn =
                    project
            };

        var memberRepository =
            new FakeProjectMemberRepository();

        memberRepository
            .ActiveMembersToReturn[
                (
                    project.Id,
                    requester.Id
                )] =
            requesterMembership;

        var permissionRepository =
            new FakeProjectMemberPermissionRepository();

        permissionRepository
            .IsActivePermissionResults[
                (
                    requesterMembership.Id,
                    ProjectPermission.ValidateTask
                )] =
            true;

        permissionRepository
            .IsActivePermissionResults[
                (
                    requesterMembership.Id,
                    ProjectPermission.SelfValidateTask
                )] =
            true;

        var taskRepository =
            new FakeProjectTaskRepository
            {
                ProjectTaskForUpdateToReturn =
                    projectTask
            };

        var unitOfWork =
            new FakeUnitOfWork();

        var handler =
            new ClaimProjectTaskValidationHandler(
                tenantRepository,
                userRepository,
                projectRepository,
                memberRepository,
                permissionRepository,
                taskRepository,
                unitOfWork);

        return new Fixture(
            tenant,
            requester,
            responsibleUser,
            project,
            projectTask,
            requesterMembership,
            tenantRepository,
            userRepository,
            projectRepository,
            memberRepository,
            permissionRepository,
            taskRepository,
            unitOfWork,
            handler);
    }

    private static ProjectTask CreateValidationTask(
        Fixture fixture,
        long responsibleUserId,
        long id)
    {
        var task =
            new ProjectTask(
                fixture.Project.Id,
                "Tarefa para validação",
                ProjectTaskPriority.Medium,
                fixture.Requester.Id,
                responsibleUserId:
                    responsibleUserId,
                dueDate:
                    new DateTime(
                        2027,
                        8,
                        31,
                        18,
                        0,
                        0,
                        DateTimeKind.Utc));

        EntityTestHelper.SetId(
            task,
            id);

        task.MoveToTodo();
        task.Start();
        task.SendToValidation();

        return task;
    }

    private static ProjectTask CreateTaskWithStatus(
        Fixture fixture,
        ProjectTaskStatus status)
    {
        var task =
            new ProjectTask(
                fixture.Project.Id,
                $"Tarefa {status}",
                ProjectTaskPriority.Medium,
                fixture.Requester.Id,
                responsibleUserId:
                    fixture.ResponsibleUser.Id);

        EntityTestHelper.SetId(
            task,
            201);

        switch (status)
        {
            case ProjectTaskStatus.Backlog:
                return task;

            case ProjectTaskStatus.Todo:
                task.MoveToTodo();
                return task;

            case ProjectTaskStatus.InProgress:
                task.MoveToTodo();
                task.Start();
                return task;

            case ProjectTaskStatus.Paused:
                task.MoveToTodo();
                task.Start();
                task.Pause(
                    "Pausa para teste.");
                return task;

            case ProjectTaskStatus.Done:
                task.MoveToTodo();
                task.Start();
                task.SendToValidation();

                task.ClaimValidation(
                    fixture.Requester.Id);

                task.ApproveValidation(
                    fixture.Requester.Id);

                return task;

            case ProjectTaskStatus.Cancelled:
                task.Cancel();
                return task;

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(status));
        }
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
                    "Pausa para teste.");

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

    private static void SetResponsibleUserId(
        ProjectTask projectTask,
        long? responsibleUserId)
    {
        var property =
            typeof(ProjectTask)
                .GetProperty(
                    nameof(ProjectTask.ResponsibleUserId),
                    BindingFlags.Instance |
                    BindingFlags.Public);

        Assert.NotNull(
            property);

        property!.SetValue(
            projectTask,
            responsibleUserId);
    }

    private static async Task AssertFailureAsync(
        Fixture fixture,
        Error expectedError,
        ClaimProjectTaskValidationCommand? command = null)
    {
        var result =
            await fixture.Handler.HandleAsync(
                command ??
                fixture.CreateCommand());

        Assert.True(
            result.IsFailure);

        Assert.False(
            result.IsSuccess);

        Assert.Null(
            result.Value);

        Assert.Equal(
            expectedError,
            result.Error);

        AssertNothingPersisted(
            fixture);
    }

    private static void AssertNothingPersisted(
        Fixture fixture)
    {
        Assert.Equal(
            0,
            fixture.UnitOfWork.SaveChangesCallCount);

        Assert.Equal(
            0,
            fixture.UnitOfWork.CommitCallCount);
    }

    private static void AssertTransactionalFailure(
        Fixture fixture)
    {
        Assert.Equal(
            1,
            fixture.UnitOfWork.BeginTransactionCallCount);

        Assert.Equal(
            0,
            fixture.UnitOfWork.SaveChangesCallCount);

        Assert.Equal(
            0,
            fixture.UnitOfWork.CommitCallCount);

        Assert.Equal(
            1,
            fixture.UnitOfWork.RollbackCallCount);
    }

    private sealed record Fixture(
        Tenant Tenant,
        User Requester,
        User ResponsibleUser,
        Project Project,
        ProjectTask ProjectTask,
        ProjectMember RequesterMembership,
        FakeTenantRepository TenantRepository,
        FakeUserRepository UserRepository,
        FakeProjectRepository ProjectRepository,
        FakeProjectMemberRepository MemberRepository,
        FakeProjectMemberPermissionRepository PermissionRepository,
        FakeProjectTaskRepository TaskRepository,
        FakeUnitOfWork UnitOfWork,
        ClaimProjectTaskValidationHandler Handler)
    {
        public ClaimProjectTaskValidationCommand CreateCommand(
            Guid? taskPublicId = null)
        {
            return new ClaimProjectTaskValidationCommand(
                Tenant.PublicId,
                Project.PublicId,
                taskPublicId ??
                    ProjectTask.PublicId,
                Requester.PublicId);
        }
    }
}