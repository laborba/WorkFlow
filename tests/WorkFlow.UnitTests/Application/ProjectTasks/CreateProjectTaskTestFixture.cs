using WorkFlow.Application.ProjectTasks.CreateProjectTask;
using WorkFlow.Domain.Entities;
using WorkFlow.Domain.Enums;
using WorkFlow.UnitTests.Application.Projects.Fakes;
using WorkFlow.UnitTests.Application.Tenants.Fakes;
using WorkFlow.UnitTests.Application.Users.Fakes;
using WorkFlow.UnitTests.Common;
using WorkFlow.UnitTests.Common.Fakes;

namespace WorkFlow.UnitTests.Application.ProjectTasks;

internal sealed class CreateProjectTaskTestFixture
{
    public Tenant Tenant { get; }
    public User Creator { get; }
    public Project Project { get; }
    public FakeTenantRepository TenantRepository { get; }
    public FakeUserRepository UserRepository { get; }
    public FakeProjectRepository ProjectRepository { get; }
    public FakeProjectMemberRepository MemberRepository { get; } = new();
    public FakeProjectMemberPermissionRepository PermissionRepository { get; } = new();
    public FakeProjectTaskRepository TaskRepository { get; } = new();
    public FakeUnitOfWork UnitOfWork { get; } = new();
    public CreateProjectTaskHandler Handler { get; }

    public CreateProjectTaskTestFixture(
        UserRole role = UserRole.TenantAdmin)
    {
        Tenant = new Tenant(
            "Empresa Tasks",
            $"REG-{Guid.NewGuid():N}",
            $"tenant-{Guid.NewGuid():N}@test.local");
        EntityTestHelper.SetId(Tenant, 10);

        Creator = new User(
            role == UserRole.SystemAdmin ? null : Tenant.Id,
            "Criador",
            $"creator-{Guid.NewGuid():N}@test.local",
            "password-hash",
            role);
        EntityTestHelper.SetId(Creator, 20);

        Project = new Project(Tenant.Id, "Projeto Tasks", 30);
        EntityTestHelper.SetId(Project, 40);

        TenantRepository = new FakeTenantRepository { TenantToReturn = Tenant };
        UserRepository = new FakeUserRepository { UserToReturn = Creator };
        ProjectRepository = new FakeProjectRepository { ProjectToReturn = Project };

        Handler = new CreateProjectTaskHandler(
            TenantRepository,
            UserRepository,
            ProjectRepository,
            MemberRepository,
            PermissionRepository,
            TaskRepository,
            UnitOfWork);
    }

    public CreateProjectTaskCommand CreateCommand()
    {
        return new CreateProjectTaskCommand(
            Tenant.PublicId,
            Project.PublicId,
            Creator.PublicId,
            "  Criar tarefa  ",
            "  Descrição da tarefa  ",
            ProjectTaskPriority.High,
            new DateTime(2027, 1, 31, 12, 0, 0, DateTimeKind.Utc));
    }

    public ProjectMember AddMembership()
    {
        var member = new ProjectMember(Project.Id, Creator.Id, 30);
        EntityTestHelper.SetId(member, 50);
        MemberRepository.ActiveMembersToReturn[(Project.Id, Creator.Id)] = member;
        return member;
    }

    public void Authorize()
    {
        var member = AddMembership();
        PermissionRepository.IsActivePermissionResults[
            (member.Id, ProjectPermission.CreateTask)] = true;
    }

    public void SetProjectStatus(ProjectStatus status)
    {
        switch (status)
        {
            case ProjectStatus.Planning:
                break;
            case ProjectStatus.InProgress:
                Project.Start();
                break;
            case ProjectStatus.Paused:
                Project.Start();
                Project.Pause("Aguardando definição.");
                break;
            case ProjectStatus.Completed:
                Project.Start();
                Project.Complete(new[] { ProjectTaskStatus.Done });
                break;
            case ProjectStatus.Archived:
                Project.Archive();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(status));
        }
    }
}
