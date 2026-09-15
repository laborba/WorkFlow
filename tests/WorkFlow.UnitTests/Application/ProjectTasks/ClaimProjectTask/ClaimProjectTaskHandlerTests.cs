using WorkFlow.Application.Common.Errors;
using WorkFlow.Application.Projects;
using WorkFlow.Application.ProjectTasks;
using WorkFlow.Application.ProjectTasks.ClaimProjectTask;
using WorkFlow.Application.Tenants;
using WorkFlow.Application.Users;
using WorkFlow.Domain.Entities;
using WorkFlow.Domain.Enums;
using WorkFlow.UnitTests.Application.Projects.Fakes;
using WorkFlow.UnitTests.Application.Tenants.Fakes;
using WorkFlow.UnitTests.Application.Users.Fakes;
using WorkFlow.UnitTests.Common;
using WorkFlow.UnitTests.Common.Fakes;

namespace WorkFlow.UnitTests.Application.ProjectTasks.ClaimProjectTask;

public sealed class ClaimProjectTaskHandlerTests
{
    [Theory]
    [InlineData(
        UserRole.TenantAdmin,
        ProjectTaskStatus.Backlog)]
    [InlineData(
        UserRole.ProjectManager,
        ProjectTaskStatus.Backlog)]
    [InlineData(
        UserRole.Member,
        ProjectTaskStatus.Backlog)]
    [InlineData(
        UserRole.Member,
        ProjectTaskStatus.Todo)]
    [InlineData(
        UserRole.Member,
        ProjectTaskStatus.Paused)]
    public async Task
        HandleAsync_ShouldClaimTask_WhenUserIsAuthorized(
            UserRole role,
            ProjectTaskStatus taskStatus)
    {
        var fixture =
            CreateFixture(
                role,
                grantClaimTask: true);

        SetTaskStatus(
            fixture.ProjectTask,
            taskStatus);

        var statusBeforeClaim =
            fixture.ProjectTask.Status;

        var statusBeforePause =
            fixture.ProjectTask.StatusBeforePause;

        var result =
            await fixture.Handler.HandleAsync(
                CreateCommand(fixture));

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Null(result.Error);

        var response =
            Assert.IsType<ClaimProjectTaskResult>(
                result.Value);

        Assert.Equal(
            fixture.Requester.Id,
            fixture.ProjectTask.ResponsibleUserId);

        Assert.Equal(
            statusBeforeClaim,
            fixture.ProjectTask.Status);

        Assert.Equal(
            statusBeforePause,
            fixture.ProjectTask.StatusBeforePause);

        Assert.NotNull(
            fixture.ProjectTask.UpdatedAt);

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
            response.ResponsibleUserPublicId);

        Assert.Equal(
            statusBeforeClaim,
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

        var membershipCall =
            Assert.Single(
                fixture.MemberRepository.GetActiveCalls);

        Assert.Equal(
            (
                fixture.Project.Id,
                fixture.Requester.Id
            ),
            membershipCall);

        if (role == UserRole.TenantAdmin)
        {
            Assert.Empty(
                fixture.PermissionRepository
                    .IsActivePermissionCalls);
        }
        else
        {
            var permissionCall =
                Assert.Single(
                    fixture.PermissionRepository
                        .IsActivePermissionCalls);

            Assert.Equal(
                (
                    fixture.ProjectMember.Id,
                    ProjectPermission.ClaimTask
                ),
                permissionCall);
        }

        var taskCall =
            Assert.Single(
                fixture.TaskRepository
                    .GetForUpdateCalls);

        Assert.Equal(
            (
                fixture.Project.Id,
                fixture.ProjectTask.PublicId
            ),
            taskCall);
    }

    [Fact]
    public async Task
        HandleAsync_ShouldThrow_WhenCommandIsNull()
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

        Assert.Equal(
            0,
            fixture.UnitOfWork.SaveChangesCallCount);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public async Task
        HandleAsync_ShouldThrow_WhenRequiredPublicIdIsEmpty(
            int publicIdToClear)
    {
        var fixture =
            CreateFixture();

        var command =
            CreateCommand(fixture) with
            {
                TenantPublicId =
                    publicIdToClear == 1
                        ? Guid.Empty
                        : fixture.Tenant.PublicId,

                ProjectPublicId =
                    publicIdToClear == 2
                        ? Guid.Empty
                        : fixture.Project.PublicId,

                TaskPublicId =
                    publicIdToClear == 3
                        ? Guid.Empty
                        : fixture.ProjectTask.PublicId,

                ClaimedByUserPublicId =
                    publicIdToClear == 4
                        ? Guid.Empty
                        : fixture.Requester.PublicId
            };

        await Assert.ThrowsAsync<ArgumentException>(
            () =>
                fixture.Handler.HandleAsync(
                    command));

        Assert.Equal(
            0,
            fixture.UnitOfWork.BeginTransactionCallCount);

        Assert.Equal(
            0,
            fixture.UnitOfWork.SaveChangesCallCount);

        Assert.Null(
            fixture.ProjectTask.ResponsibleUserId);
    }

    [Fact]
    public async Task
        HandleAsync_ShouldReturnFailure_WhenTenantDoesNotExist()
    {
        var fixture =
            CreateFixture();

        fixture.TenantRepository.TenantToReturn =
            null;

        var result =
            await fixture.Handler.HandleAsync(
                CreateCommand(fixture));

        AssertFailure(
            result,
            TenantErrors.NotFound);

        Assert.Equal(
            0,
            fixture.UnitOfWork.BeginTransactionCallCount);

        Assert.Equal(
            0,
            fixture.UnitOfWork.SaveChangesCallCount);

        Assert.Null(
            fixture.ProjectTask.ResponsibleUserId);
    }

    [Fact]
    public async Task
        HandleAsync_ShouldReturnFailure_WhenTenantIsInactive()
    {
        var fixture =
            CreateFixture();

        fixture.Tenant.Deactivate();

        var result =
            await fixture.Handler.HandleAsync(
                CreateCommand(fixture));

        AssertFailure(
            result,
            TenantErrors.Inactive);

        Assert.Equal(
            0,
            fixture.UnitOfWork.BeginTransactionCallCount);

        Assert.Equal(
            0,
            fixture.UnitOfWork.SaveChangesCallCount);

        Assert.Null(
            fixture.ProjectTask.ResponsibleUserId);
    }

    [Fact]
    public async Task
        HandleAsync_ShouldReturnFailure_WhenRequesterDoesNotExist()
    {
        var fixture =
            CreateFixture();

        fixture.UserRepository
            .UsersByPublicIdToReturn
            .Remove(
                fixture.Requester.PublicId);

        var result =
            await fixture.Handler.HandleAsync(
                CreateCommand(fixture));

        AssertFailure(
            result,
            UserErrors.NotFound);

        Assert.Equal(
            0,
            fixture.UnitOfWork.BeginTransactionCallCount);

        Assert.Equal(
            0,
            fixture.UnitOfWork.SaveChangesCallCount);

        Assert.Null(
            fixture.ProjectTask.ResponsibleUserId);
    }

    [Fact]
    public async Task
        HandleAsync_ShouldReturnFailure_WhenRequesterIsInactive()
    {
        var fixture =
            CreateFixture();

        fixture.Requester.Deactivate();

        var result =
            await fixture.Handler.HandleAsync(
                CreateCommand(fixture));

        AssertFailure(
            result,
            UserErrors.Inactive);

        Assert.Equal(
            0,
            fixture.UnitOfWork.BeginTransactionCallCount);

        Assert.Equal(
            0,
            fixture.UnitOfWork.SaveChangesCallCount);

        Assert.Null(
            fixture.ProjectTask.ResponsibleUserId);
    }

    [Fact]
    public async Task
        HandleAsync_ShouldReturnFailure_WhenProjectDoesNotExist()
    {
        var fixture =
            CreateFixture();

        fixture.ProjectRepository.ProjectToReturn =
            null;

        var result =
            await fixture.Handler.HandleAsync(
                CreateCommand(fixture));

        AssertFailure(
            result,
            ProjectErrors.NotFound);

        AssertTransactionalFailure(
            fixture);

        Assert.Null(
            fixture.ProjectTask.ResponsibleUserId);
    }

    [Theory]
    [InlineData(UserRole.TenantAdmin)]
    [InlineData(UserRole.ProjectManager)]
    [InlineData(UserRole.Member)]
    public async Task
        HandleAsync_ShouldReturnClaimNotAllowed_WhenRequesterIsNotActiveMember(
            UserRole role)
    {
        var fixture =
            CreateFixture(
                role,
                grantClaimTask: true);

        fixture.MemberRepository
            .ActiveMembersToReturn[
                (
                    fixture.Project.Id,
                    fixture.Requester.Id
                )] =
            null;

        var result =
            await fixture.Handler.HandleAsync(
                CreateCommand(fixture));

        AssertFailure(
            result,
            ProjectTaskErrors.ClaimNotAllowed);

        AssertTransactionalFailure(
            fixture);

        Assert.Empty(
            fixture.TaskRepository
                .GetForUpdateCalls);

        Assert.Null(
            fixture.ProjectTask.ResponsibleUserId);
    }

    [Theory]
    [InlineData(UserRole.ProjectManager)]
    [InlineData(UserRole.Member)]
    public async Task
        HandleAsync_ShouldReturnClaimNotAllowed_WhenRequesterDoesNotHaveClaimTask(
            UserRole role)
    {
        var fixture =
            CreateFixture(
                role,
                grantClaimTask: false);

        var result =
            await fixture.Handler.HandleAsync(
                CreateCommand(fixture));

        AssertFailure(
            result,
            ProjectTaskErrors.ClaimNotAllowed);

        AssertTransactionalFailure(
            fixture);

        var permissionCall =
            Assert.Single(
                fixture.PermissionRepository
                    .IsActivePermissionCalls);

        Assert.Equal(
            (
                fixture.ProjectMember.Id,
                ProjectPermission.ClaimTask
            ),
            permissionCall);

        Assert.Empty(
            fixture.TaskRepository
                .GetForUpdateCalls);

        Assert.Null(
            fixture.ProjectTask.ResponsibleUserId);
    }

    [Theory]
    [InlineData(ProjectStatus.Completed)]
    [InlineData(ProjectStatus.Archived)]
    public async Task
        HandleAsync_ShouldReturnFailure_WhenProjectStatusBlocksClaim(
            ProjectStatus projectStatus)
    {
        var fixture =
            CreateFixture();

        SetProjectStatus(
            fixture.Project,
            projectStatus);

        var result =
            await fixture.Handler.HandleAsync(
                CreateCommand(fixture));

        AssertFailure(
            result,
            ProjectTaskErrors.ClaimBlockedByProjectStatus);

        AssertTransactionalFailure(
            fixture);

        Assert.Empty(
            fixture.TaskRepository
                .GetForUpdateCalls);

        Assert.Null(
            fixture.ProjectTask.ResponsibleUserId);
    }

    [Fact]
    public async Task
        HandleAsync_ShouldReturnNotFound_WhenTaskDoesNotExist()
    {
        var fixture =
            CreateFixture();

        fixture.TaskRepository
            .ProjectTaskForUpdateToReturn =
            null;

        var result =
            await fixture.Handler.HandleAsync(
                CreateCommand(fixture));

        AssertFailure(
            result,
            ProjectTaskErrors.NotFound);

        AssertTransactionalFailure(
            fixture);

        var taskCall =
            Assert.Single(
                fixture.TaskRepository
                    .GetForUpdateCalls);

        Assert.Equal(
            (
                fixture.Project.Id,
                fixture.ProjectTask.PublicId
            ),
            taskCall);
    }

    [Fact]
    public async Task
        HandleAsync_ShouldReturnArchived_WhenTaskIsArchived()
    {
        var fixture =
            CreateFixture();

        fixture.ProjectTask.Cancel();
        fixture.ProjectTask.Archive();

        var updatedAtBeforeClaim =
            fixture.ProjectTask.UpdatedAt;

        var result =
            await fixture.Handler.HandleAsync(
                CreateCommand(fixture));

        AssertFailure(
            result,
            ProjectTaskErrors.Archived);

        AssertTransactionalFailure(
            fixture);

        Assert.Null(
            fixture.ProjectTask.ResponsibleUserId);

        Assert.Equal(
            updatedAtBeforeClaim,
            fixture.ProjectTask.UpdatedAt);
    }

    [Theory]
    [InlineData(ProjectTaskStatus.InProgress)]
    [InlineData(ProjectTaskStatus.Validation)]
    [InlineData(ProjectTaskStatus.Done)]
    [InlineData(ProjectTaskStatus.Cancelled)]
    public async Task
        HandleAsync_ShouldReturnFailure_WhenTaskStatusBlocksClaim(
            ProjectTaskStatus taskStatus)
    {
        var fixture =
            CreateFixture();

        SetTaskStatus(
            fixture.ProjectTask,
            taskStatus);

        var responsibleBeforeClaim =
            fixture.ProjectTask.ResponsibleUserId;

        var updatedAtBeforeClaim =
            fixture.ProjectTask.UpdatedAt;

        var result =
            await fixture.Handler.HandleAsync(
                CreateCommand(fixture));

        AssertFailure(
            result,
            ProjectTaskErrors.ClaimBlockedByTaskStatus);

        AssertTransactionalFailure(
            fixture);

        Assert.Equal(
            responsibleBeforeClaim,
            fixture.ProjectTask.ResponsibleUserId);

        Assert.Equal(
            taskStatus,
            fixture.ProjectTask.Status);

        Assert.Equal(
            updatedAtBeforeClaim,
            fixture.ProjectTask.UpdatedAt);
    }

    [Fact]
    public async Task
        HandleAsync_ShouldReturnAlreadyAssigned_WhenTaskAlreadyHasResponsible()
    {
        var fixture =
            CreateFixture();

        fixture.ProjectTask.AssignResponsible(
            999);

        var updatedAtBeforeClaim =
            fixture.ProjectTask.UpdatedAt;

        var result =
            await fixture.Handler.HandleAsync(
                CreateCommand(fixture));

        AssertFailure(
            result,
            ProjectTaskErrors.AlreadyAssigned);

        AssertTransactionalFailure(
            fixture);

        Assert.Equal(
            999,
            fixture.ProjectTask.ResponsibleUserId);

        Assert.Equal(
            ProjectTaskStatus.Backlog,
            fixture.ProjectTask.Status);

        Assert.Equal(
            updatedAtBeforeClaim,
            fixture.ProjectTask.UpdatedAt);
    }

    [Fact]
    public async Task
        HandleAsync_ShouldRollback_WhenSaveChangesFails()
    {
        var fixture =
            CreateFixture();

        fixture.UnitOfWork.OnSaveChanges =
            _ =>
                throw new InvalidOperationException(
                    "Falha simulada ao salvar claim.");

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () =>
                    fixture.Handler.HandleAsync(
                        CreateCommand(fixture)));

        Assert.Equal(
            "Falha simulada ao salvar claim.",
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
        UserRole requesterRole = UserRole.TenantAdmin,
        bool grantClaimTask = true)
    {
        var tenant =
            new Tenant(
                "Empresa Claim Task",
                $"REG-{Guid.NewGuid():N}",
                $"tenant-{Guid.NewGuid():N}@test.local");

        EntityTestHelper.SetId(
            tenant,
            42);

        var requester =
            new User(
                tenant.Id,
                "Usuário Claim",
                $"claim-{Guid.NewGuid():N}@test.local",
                "password-hash",
                requesterRole);

        EntityTestHelper.SetId(
            requester,
            10);

        var project =
            new Project(
                tenant.Id,
                "Projeto Claim Task",
                requester.Id);

        EntityTestHelper.SetId(
            project,
            100);

        var projectTask =
            new ProjectTask(
                project.Id,
                "Tarefa disponível",
                ProjectTaskPriority.Medium,
                requester.Id);

        EntityTestHelper.SetId(
            projectTask,
            200);

        var projectMember =
            new ProjectMember(
                project.Id,
                requester.Id,
                requester.Id);

        EntityTestHelper.SetId(
            projectMember,
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
            projectMember;

        var permissionRepository =
            new FakeProjectMemberPermissionRepository();

        if (requesterRole != UserRole.TenantAdmin)
        {
            permissionRepository
                .IsActivePermissionResults[
                    (
                        projectMember.Id,
                        ProjectPermission.ClaimTask
                    )] =
                grantClaimTask;
        }

        var taskRepository =
            new FakeProjectTaskRepository
            {
                ProjectTaskForUpdateToReturn =
                    projectTask
            };

        var unitOfWork =
            new FakeUnitOfWork();

        var handler =
            new ClaimProjectTaskHandler(
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
            project,
            projectTask,
            projectMember,
            tenantRepository,
            userRepository,
            projectRepository,
            memberRepository,
            permissionRepository,
            taskRepository,
            unitOfWork,
            handler);
    }

    private static ClaimProjectTaskCommand CreateCommand(
        Fixture fixture)
    {
        return new ClaimProjectTaskCommand(
            fixture.Tenant.PublicId,
            fixture.Project.PublicId,
            fixture.ProjectTask.PublicId,
            fixture.Requester.PublicId);
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

    private static void SetTaskStatus(
        ProjectTask projectTask,
        ProjectTaskStatus status)
    {
        switch (status)
        {
            case ProjectTaskStatus.Backlog:
                return;

            case ProjectTaskStatus.Todo:
                projectTask.MoveToTodo();
                return;

            case ProjectTaskStatus.Paused:
                projectTask.MoveToTodo();
                projectTask.Pause(
                    "Pausa para teste.");
                return;

            case ProjectTaskStatus.InProgress:
                projectTask.AssignResponsible(
                    999);
                projectTask.MoveToTodo();
                projectTask.Start();
                return;

            case ProjectTaskStatus.Validation:
                projectTask.AssignResponsible(
                    999);
                projectTask.MoveToTodo();
                projectTask.Start();
                projectTask.SendToValidation();
                return;

            case ProjectTaskStatus.Done:
                projectTask.AssignResponsible(
                    999);
                projectTask.MoveToTodo();
                projectTask.Start();
                projectTask.SendToValidation();
                projectTask.ClaimValidation(
                    998);
                projectTask.ApproveValidation(
                    998);
                return;

            case ProjectTaskStatus.Cancelled:
                projectTask.Cancel();
                return;

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(status));
        }
    }

    private static void AssertFailure(
        WorkFlow.Application.Common.Results.Result<ClaimProjectTaskResult> result,
        Error expectedError)
    {
        Assert.True(result.IsFailure);
        Assert.False(result.IsSuccess);
        Assert.Equal(
            expectedError,
            result.Error);
        Assert.Null(
            result.Value);
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
        Project Project,
        ProjectTask ProjectTask,
        ProjectMember ProjectMember,
        FakeTenantRepository TenantRepository,
        FakeUserRepository UserRepository,
        FakeProjectRepository ProjectRepository,
        FakeProjectMemberRepository MemberRepository,
        FakeProjectMemberPermissionRepository PermissionRepository,
        FakeProjectTaskRepository TaskRepository,
        FakeUnitOfWork UnitOfWork,
        ClaimProjectTaskHandler Handler);
}