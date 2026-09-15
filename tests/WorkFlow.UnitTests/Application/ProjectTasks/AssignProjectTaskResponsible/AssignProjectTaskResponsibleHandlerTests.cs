using WorkFlow.Application.Common.Errors;
using WorkFlow.Application.Projects;
using WorkFlow.Application.ProjectTasks;
using WorkFlow.Application.ProjectTasks.AssignProjectTaskResponsible;
using WorkFlow.Application.Tenants;
using WorkFlow.Application.Users;
using WorkFlow.Domain.Entities;
using WorkFlow.Domain.Enums;
using WorkFlow.UnitTests.Application.Projects.Fakes;
using WorkFlow.UnitTests.Application.Tenants.Fakes;
using WorkFlow.UnitTests.Application.Users.Fakes;
using WorkFlow.UnitTests.Common;
using WorkFlow.UnitTests.Common.Fakes;

namespace WorkFlow.UnitTests.Application.ProjectTasks.AssignProjectTaskResponsible;

public sealed class AssignProjectTaskResponsibleHandlerTests
{
    [Fact]
    public async Task HandleAsync_ShouldThrow_WhenCommandIsNull()
    {
        var fixture =
            CreateFixture();

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => fixture.Handler.HandleAsync(
                null!));

        AssertNothingPersisted(
            fixture);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
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

                4 => command with
                {
                    RequestedByUserPublicId = Guid.Empty
                },

                _ => command with
                {
                    ResponsibleUserPublicId = Guid.Empty
                }
            };

        await Assert.ThrowsAsync<ArgumentException>(
            () => fixture.Handler.HandleAsync(
                command));

        AssertNothingPersisted(
            fixture);

        Assert.Equal(
            0,
            fixture.UnitOfWork.BeginTransactionCallCount);
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

        Assert.Equal(
            1,
            fixture.UnitOfWork.RollbackCallCount);
    }

    [Theory]
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
            ProjectTaskErrors.AssignmentNotAllowed);

        Assert.Equal(
            1,
            fixture.UnitOfWork.RollbackCallCount);

        Assert.Empty(
            fixture.PermissionRepository
                .IsActivePermissionCalls);
    }

    [Theory]
    [InlineData(UserRole.ProjectManager)]
    [InlineData(UserRole.Member)]
    public async Task HandleAsync_ShouldRejectRequester_WhenAssignTaskPermissionIsMissing(
        UserRole role)
    {
        var fixture =
            CreateFixture(
                role);

        fixture.PermissionRepository
            .IsActivePermissionResults[
                (
                    fixture.RequesterMembership.Id,
                    ProjectPermission.AssignTask
                )] =
            false;

        await AssertFailureAsync(
            fixture,
            ProjectTaskErrors.AssignmentNotAllowed);

        Assert.Equal(
            (
                fixture.RequesterMembership.Id,
                ProjectPermission.AssignTask
            ),
            Assert.Single(
                fixture.PermissionRepository
                    .IsActivePermissionCalls));

        Assert.Equal(
            1,
            fixture.UnitOfWork.RollbackCallCount);
    }

    [Theory]
    [InlineData(ProjectStatus.Completed)]
    [InlineData(ProjectStatus.Archived)]
    public async Task HandleAsync_ShouldRejectAssignment_WhenProjectStatusBlocksIt(
        ProjectStatus status)
    {
        var fixture =
            CreateFixture();

        SetProjectStatus(
            fixture.Project,
            status);

        await AssertFailureAsync(
            fixture,
            ProjectTaskErrors
                .AssignmentBlockedByProjectStatus);

        Assert.Equal(
            status,
            fixture.Project.Status);

        Assert.Equal(
            1,
            fixture.UnitOfWork.RollbackCallCount);
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

        Assert.Equal(
            1,
            fixture.UnitOfWork.RollbackCallCount);
    }

    [Fact]
    public async Task HandleAsync_ShouldRejectAssignment_WhenTaskIsArchived()
    {
        var fixture =
            CreateFixture();

        fixture.ProjectTask.Cancel();
        fixture.ProjectTask.Archive();

        await AssertFailureAsync(
            fixture,
            ProjectTaskErrors.Archived);

        Assert.Equal(
            1,
            fixture.UnitOfWork.RollbackCallCount);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnNotFound_WhenResponsibleUserDoesNotExist()
    {
        var fixture =
            CreateFixture();

        fixture.UserRepository
            .UsersByPublicIdToReturn
            .Remove(
                fixture.ResponsibleUser.PublicId);

        await AssertFailureAsync(
            fixture,
            ProjectTaskErrors.ResponsibleUserNotFound);

        Assert.Equal(
            1,
            fixture.UnitOfWork.RollbackCallCount);
    }

    [Fact]
    public async Task HandleAsync_ShouldRejectInactiveResponsibleUser()
    {
        var fixture =
            CreateFixture();

        fixture.ResponsibleUser.Deactivate();

        await AssertFailureAsync(
            fixture,
            ProjectTaskErrors.ResponsibleUserInactive);

        Assert.Equal(
            1,
            fixture.UnitOfWork.RollbackCallCount);
    }

    [Fact]
    public async Task HandleAsync_ShouldRejectResponsibleUser_WhenMembershipIsNotActive()
    {
        var fixture =
            CreateFixture();

        fixture.MemberRepository
            .ActiveMembersToReturn[
                (
                    fixture.Project.Id,
                    fixture.ResponsibleUser.Id
                )] =
            null;

        await AssertFailureAsync(
            fixture,
            ProjectTaskErrors
                .ResponsibleUserNotActiveMember);

        Assert.Equal(
            1,
            fixture.UnitOfWork.RollbackCallCount);
    }

    [Fact]
    public async Task HandleAsync_ShouldAssignResponsible_WhenRequesterIsTenantAdmin()
    {
        var fixture =
            CreateFixture(
                UserRole.TenantAdmin);

        var originalStatus =
            fixture.ProjectTask.Status;

        var before =
            DateTime.UtcNow;

        var result =
            await fixture.Handler.HandleAsync(
                fixture.CreateCommand());

        Assert.True(
            result.IsSuccess);

        Assert.Null(
            result.Error);

        var response =
            Assert.IsType<AssignProjectTaskResponsibleResult>(
                result.Value);

        Assert.Equal(
            fixture.ResponsibleUser.Id,
            fixture.ProjectTask.ResponsibleUserId);

        Assert.Equal(
            fixture.ResponsibleUser.PublicId,
            response.ResponsibleUserPublicId);

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
            originalStatus,
            fixture.ProjectTask.Status);

        Assert.Equal(
            originalStatus,
            response.Status);

        Assert.NotNull(
            fixture.ProjectTask.UpdatedAt);

        Assert.InRange(
            fixture.ProjectTask.UpdatedAt!.Value,
            before,
            DateTime.UtcNow);

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
    }

    [Theory]
    [InlineData(UserRole.ProjectManager)]
    [InlineData(UserRole.Member)]
    public async Task HandleAsync_ShouldAssignResponsible_WhenRequesterHasAssignTaskPermission(
        UserRole role)
    {
        var fixture =
            CreateFixture(
                role);

        var result =
            await fixture.Handler.HandleAsync(
                fixture.CreateCommand());

        Assert.True(
            result.IsSuccess);

        Assert.Equal(
            fixture.ResponsibleUser.Id,
            fixture.ProjectTask.ResponsibleUserId);

        Assert.Equal(
            (
                fixture.RequesterMembership.Id,
                ProjectPermission.AssignTask
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

    [Fact]
    public async Task HandleAsync_ShouldReassignResponsible_WithoutChangingTaskStatus()
    {
        var fixture =
            CreateFixture();

        fixture.ProjectTask.AssignResponsible(
            999);

        fixture.ProjectTask.MoveToTodo();
        fixture.ProjectTask.Start();

        Assert.Equal(
            ProjectTaskStatus.InProgress,
            fixture.ProjectTask.Status);

        var result =
            await fixture.Handler.HandleAsync(
                fixture.CreateCommand());

        Assert.True(
            result.IsSuccess);

        Assert.Equal(
            fixture.ResponsibleUser.Id,
            fixture.ProjectTask.ResponsibleUserId);

        Assert.Equal(
            ProjectTaskStatus.InProgress,
            fixture.ProjectTask.Status);

        Assert.Equal(
            ProjectTaskStatus.InProgress,
            result.Value!.Status);
    }

    [Fact]
    public async Task HandleAsync_ShouldRollback_WhenSaveChangesFails()
    {
        var fixture =
            CreateFixture();

        fixture.UnitOfWork.OnSaveChanges =
            _ =>
                throw new InvalidOperationException(
                    "Falha simulada ao salvar atribuição.");

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () =>
                    fixture.Handler.HandleAsync(
                        fixture.CreateCommand()));

        Assert.Equal(
            "Falha simulada ao salvar atribuição.",
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

    private static async Task AssertFailureAsync(
        Fixture fixture,
        Error expectedError)
    {
        var result =
            await fixture.Handler.HandleAsync(
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

    private static Fixture CreateFixture(
        UserRole requesterRole = UserRole.TenantAdmin)
    {
        var tenant =
            new Tenant(
                "Empresa Assign Task",
                $"REG-{Guid.NewGuid():N}",
                $"tenant-{Guid.NewGuid():N}@test.local");

        EntityTestHelper.SetId(
            tenant,
            42);

        var requester =
            new User(
                tenant.Id,
                "Usuário Solicitante",
                $"requester-{Guid.NewGuid():N}@test.local",
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
                "Projeto Assign Task",
                requester.Id);

        EntityTestHelper.SetId(
            project,
            100);

        var projectTask =
            new ProjectTask(
                project.Id,
                "Tarefa de atribuição",
                ProjectTaskPriority.Medium,
                requester.Id);

        EntityTestHelper.SetId(
            projectTask,
            200);

        var requesterMembership =
            new ProjectMember(
                project.Id,
                requester.Id,
                requester.Id);

        EntityTestHelper.SetId(
            requesterMembership,
            300);

        var responsibleMembership =
            new ProjectMember(
                project.Id,
                responsibleUser.Id,
                requester.Id);

        EntityTestHelper.SetId(
            responsibleMembership,
            301);

        var tenantRepository =
            new FakeTenantRepository
            {
                TenantToReturn = tenant
            };

        var userRepository =
            new FakeUserRepository();

        userRepository
            .UsersByPublicIdToReturn[
                requester.PublicId] =
            requester;

        userRepository
            .UsersByPublicIdToReturn[
                responsibleUser.PublicId] =
            responsibleUser;

        var projectRepository =
            new FakeProjectRepository
            {
                ProjectToReturn = project
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

        memberRepository
            .ActiveMembersToReturn[
                (
                    project.Id,
                    responsibleUser.Id
                )] =
            responsibleMembership;

        var permissionRepository =
            new FakeProjectMemberPermissionRepository();

        permissionRepository
            .IsActivePermissionResults[
                (
                    requesterMembership.Id,
                    ProjectPermission.AssignTask
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
            new AssignProjectTaskResponsibleHandler(
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
            responsibleMembership,
            tenantRepository,
            userRepository,
            projectRepository,
            memberRepository,
            permissionRepository,
            taskRepository,
            unitOfWork,
            handler);
    }

    private static void SetProjectStatus(
        Project project,
        ProjectStatus status)
    {
        switch (status)
        {
            case ProjectStatus.Completed:
                project.Start();

                project.Complete(
                    new[]
                    {
                        ProjectTaskStatus.Done
                    });

                break;

            case ProjectStatus.Archived:
                project.Archive();
                break;

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(status));
        }
    }

    private sealed record Fixture(
        Tenant Tenant,
        User Requester,
        User ResponsibleUser,
        Project Project,
        ProjectTask ProjectTask,
        ProjectMember RequesterMembership,
        ProjectMember ResponsibleMembership,
        FakeTenantRepository TenantRepository,
        FakeUserRepository UserRepository,
        FakeProjectRepository ProjectRepository,
        FakeProjectMemberRepository MemberRepository,
        FakeProjectMemberPermissionRepository PermissionRepository,
        FakeProjectTaskRepository TaskRepository,
        FakeUnitOfWork UnitOfWork,
        AssignProjectTaskResponsibleHandler Handler)
    {
        public AssignProjectTaskResponsibleCommand CreateCommand()
        {
            return new AssignProjectTaskResponsibleCommand(
                Tenant.PublicId,
                Project.PublicId,
                ProjectTask.PublicId,
                Requester.PublicId,
                ResponsibleUser.PublicId);
        }
    }
}