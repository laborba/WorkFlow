using WorkFlow.Domain.Entities;
using WorkFlow.Domain.Enums;
using WorkFlow.UnitTests.Application.Projects.Fakes;
using WorkFlow.UnitTests.Application.Tenants.Fakes;
using WorkFlow.UnitTests.Application.Users.Fakes;
using WorkFlow.UnitTests.Common;
using WorkFlow.UnitTests.Common.Fakes;

namespace WorkFlow.UnitTests.Application.Projects;

internal sealed class ProjectStatusHandlerTestFixture
{
    public Tenant Tenant { get; }

    public User Requester { get; }

    public Project Project { get; }

    public FakeTenantRepository TenantRepository { get; }

    public FakeUserRepository UserRepository { get; }

    public FakeProjectRepository ProjectRepository { get; }

    public FakeProjectMemberRepository ProjectMemberRepository { get; }

    public FakeProjectMemberPermissionRepository
        PermissionRepository
    { get; }

    public FakeProjectTaskRepository ProjectTaskRepository { get; }

    public FakeUnitOfWork UnitOfWork { get; }

    public ProjectStatusHandlerTestFixture(
        UserRole requesterRole = UserRole.TenantAdmin)
    {
        Tenant =
            new Tenant(
                "Empresa Status Project",
                $"REG-{Guid.NewGuid():N}",
                $"tenant-{Guid.NewGuid():N}@test.local");

        EntityTestHelper.SetId(
            Tenant,
            10);

        Requester =
            new User(
                Tenant.Id,
                "Usuário Solicitante",
                $"requester-{Guid.NewGuid():N}@test.local",
                "password-hash",
                requesterRole);

        EntityTestHelper.SetId(
            Requester,
            20);

        Project =
            new Project(
                Tenant.Id,
                "Projeto Status",
                Requester.Id,
                "Projeto utilizado nos testes de status.",
                dueDate:
                    new DateTime(
                        2027,
                        1,
                        31,
                        12,
                        0,
                        0,
                        DateTimeKind.Utc));

        EntityTestHelper.SetId(
            Project,
            30);

        TenantRepository =
            new FakeTenantRepository
            {
                TenantToReturn = Tenant
            };

        UserRepository =
            new FakeUserRepository
            {
                UserToReturn = Requester
            };

        ProjectRepository =
            new FakeProjectRepository
            {
                ProjectToReturn = Project
            };

        ProjectMemberRepository =
            new FakeProjectMemberRepository();

        PermissionRepository =
            new FakeProjectMemberPermissionRepository();

        ProjectTaskRepository =
            new FakeProjectTaskRepository();

        UnitOfWork =
            new FakeUnitOfWork();
    }

    public void AddActiveMembership()
    {
        var projectMember =
            new ProjectMember(
                Project.Id,
                Requester.Id,
                Requester.Id);

        EntityTestHelper.SetId(
            projectMember,
            40);

        ProjectMemberRepository.ActiveMemberToReturn =
            projectMember;
    }

    public void Authorize(
        ProjectPermission permission)
    {
        AddActiveMembership();

        PermissionRepository.IsActivePermissionResult =
            true;
    }

    public void MoveProjectToInProgress()
    {
        Project.Start();
    }

    public void MoveProjectToCompleted()
    {
        Project.Start();

        Project.Complete(
            new[]
            {
                ProjectTaskStatus.Done
            });
    }

    public void ArchivePlanningProject()
    {
        Project.Archive();
    }

    public void ArchiveCompletedProject()
    {
        MoveProjectToCompleted();

        Project.Archive();
    }
}