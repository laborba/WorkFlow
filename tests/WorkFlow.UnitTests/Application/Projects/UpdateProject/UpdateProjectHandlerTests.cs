using WorkFlow.Application.Projects;
using WorkFlow.Application.Projects.UpdateProject;
using WorkFlow.Application.Tenants;
using WorkFlow.Application.Users;
using WorkFlow.Domain.Entities;
using WorkFlow.Domain.Enums;
using WorkFlow.UnitTests.Application.Projects.Fakes;
using WorkFlow.UnitTests.Application.Tenants.Fakes;
using WorkFlow.UnitTests.Application.Users.Fakes;
using WorkFlow.UnitTests.Common;

namespace WorkFlow.UnitTests.Application.Projects.UpdateProject;

public sealed class UpdateProjectHandlerTests
{
    [Fact]
    public async Task
    HandleAsync_ShouldUpdateProject_WhenRequesterIsTenantAdmin()
    {
        var tenant =
            CreatePersistedTenant();

        var requester =
            CreatePersistedUser(
                tenant.Id,
                10,
                UserRole.TenantAdmin);

        var creator =
            CreatePersistedUser(
                tenant.Id,
                20,
                UserRole.ProjectManager);

        var project =
            CreatePersistedProject(
                tenant.Id,
                creator.Id);

        var tenantRepository =
            new FakeTenantRepository
            {
                TenantToReturn = tenant
            };

        var userRepository =
            new FakeUserRepository
            {
                UserToReturn = requester
            };

        userRepository.UsersByIdToReturn.Add(
            creator.Id,
            creator);

        var projectRepository =
            new FakeProjectRepository
            {
                ProjectToReturn = project
            };

        var projectMemberRepository =
            new FakeProjectMemberRepository();

        var unitOfWork =
            new FakeUnitOfWork();

        var handler =
            new UpdateProjectHandler(
                tenantRepository,
                userRepository,
                projectRepository,
                projectMemberRepository,
                unitOfWork);

        var newDueDate =
            new DateTime(
                2026,
                12,
                15,
                12,
                0,
                0,
                DateTimeKind.Utc);

        var command =
            new UpdateProjectCommand(
                tenant.PublicId,
                project.PublicId,
                requester.PublicId,
                "Projeto Atualizado",
                "Descrição atualizada",
                newDueDate);

        var result =
            await handler.HandleAsync(command);

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Null(result.Error);

        var response =
            Assert.IsType<UpdateProjectResult>(
                result.Value);

        Assert.Equal(
            "Projeto Atualizado",
            project.Name);

        Assert.Equal(
            "Descrição atualizada",
            project.Description);

        Assert.Equal(
            newDueDate,
            project.DueDate);

        Assert.NotNull(
            project.UpdatedAt);

        Assert.Equal(
            project.PublicId,
            response.PublicId);

        Assert.Equal(
            tenant.PublicId,
            response.TenantPublicId);

        Assert.Equal(
            creator.PublicId,
            response.CreatedByUserPublicId);

        Assert.Null(
            response.ResponsibleUserPublicId);

        Assert.Equal(
            project.Name,
            response.Name);

        Assert.Equal(
            project.Description,
            response.Description);

        Assert.Equal(
            project.Status,
            response.Status);

        Assert.Equal(
            project.DueDate,
            response.DueDate);

        Assert.Equal(
            1,
            unitOfWork.SaveChangesCallCount);

        Assert.Null(
            projectMemberRepository.QueriedProjectId);

        Assert.Null(
            projectMemberRepository.QueriedUserId);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldUpdateProject_WhenRequesterIsActiveProjectManagerMember()
    {
        var tenant =
            CreatePersistedTenant();

        var requester =
            CreatePersistedUser(
                tenant.Id,
                10,
                UserRole.ProjectManager);

        var creator =
            CreatePersistedUser(
                tenant.Id,
                20,
                UserRole.TenantAdmin);

        var project =
            CreatePersistedProject(
                tenant.Id,
                creator.Id);

        var tenantRepository =
            new FakeTenantRepository
            {
                TenantToReturn = tenant
            };

        var userRepository =
            new FakeUserRepository
            {
                UserToReturn = requester
            };

        userRepository.UsersByIdToReturn.Add(
            creator.Id,
            creator);

        var projectRepository =
            new FakeProjectRepository
            {
                ProjectToReturn = project
            };

        var projectMemberRepository =
            new FakeProjectMemberRepository
            {
                IsActiveMemberResult = true
            };

        var unitOfWork =
            new FakeUnitOfWork();

        var handler =
            new UpdateProjectHandler(
                tenantRepository,
                userRepository,
                projectRepository,
                projectMemberRepository,
                unitOfWork);

        var command =
            new UpdateProjectCommand(
                tenant.PublicId,
                project.PublicId,
                requester.PublicId,
                "Projeto alterado pelo gerente",
                null,
                null);

        var result =
            await handler.HandleAsync(command);

        Assert.True(result.IsSuccess);

        Assert.Equal(
            "Projeto alterado pelo gerente",
            project.Name);

        Assert.Null(
            project.Description);

        Assert.Null(
            project.DueDate);

        Assert.Equal(
            project.Id,
            projectMemberRepository.QueriedProjectId);

        Assert.Equal(
            requester.Id,
            projectMemberRepository.QueriedUserId);

        Assert.Equal(
            1,
            unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldReturnFailure_WhenProjectManagerIsNotActiveMember()
    {
        var fixture =
            CreateFixture(
                UserRole.ProjectManager);

        fixture.ProjectMemberRepository
            .IsActiveMemberResult = false;

        var result =
            await fixture.Handler.HandleAsync(
                CreateCommand(fixture));

        Assert.True(result.IsFailure);

        Assert.Equal(
            ProjectErrors.UpdateNotAllowed,
            result.Error);

        Assert.Equal(
            fixture.Project.Id,
            fixture.ProjectMemberRepository.QueriedProjectId);

        Assert.Equal(
            fixture.Requester.Id,
            fixture.ProjectMemberRepository.QueriedUserId);

        Assert.Equal(
            0,
            fixture.UnitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldReturnFailure_WhenRequesterIsMember()
    {
        var fixture =
            CreateFixture(
                UserRole.Member);

        var result =
            await fixture.Handler.HandleAsync(
                CreateCommand(fixture));

        Assert.True(result.IsFailure);

        Assert.Equal(
            ProjectErrors.UpdateNotAllowed,
            result.Error);

        Assert.Null(
            fixture.ProjectMemberRepository.QueriedProjectId);

        Assert.Null(
            fixture.ProjectMemberRepository.QueriedUserId);

        Assert.Equal(
            0,
            fixture.UnitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldUpdateProject_WhenProjectIsCompleted()
    {
        var fixture =
            CreateFixture(
                UserRole.TenantAdmin);

        fixture.Project.Start();

        fixture.Project.Complete(
            new[]
            {
                ProjectTaskStatus.Done
            });

        var result =
            await fixture.Handler.HandleAsync(
                new UpdateProjectCommand(
                    fixture.Tenant.PublicId,
                    fixture.Project.PublicId,
                    fixture.Requester.PublicId,
                    "Projeto concluído atualizado",
                    "Correção administrativa",
                    null));

        Assert.True(result.IsSuccess);

        Assert.Equal(
            ProjectStatus.Completed,
            fixture.Project.Status);

        Assert.Equal(
            "Projeto concluído atualizado",
            fixture.Project.Name);

        Assert.Equal(
            "Correção administrativa",
            fixture.Project.Description);

        Assert.Equal(
            1,
            fixture.UnitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldReturnFailure_WhenProjectIsArchived()
    {
        var fixture =
            CreateFixture(
                UserRole.TenantAdmin);

        fixture.Project.Archive();

        var originalName =
            fixture.Project.Name;

        var result =
            await fixture.Handler.HandleAsync(
                CreateCommand(fixture));

        Assert.True(result.IsFailure);

        Assert.Equal(
            ProjectErrors.Archived,
            result.Error);

        Assert.Equal(
            originalName,
            fixture.Project.Name);

        Assert.Equal(
            0,
            fixture.UnitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldReturnFailure_WhenTenantDoesNotExist()
    {
        var tenantRepository =
            new FakeTenantRepository();

        var userRepository =
            new FakeUserRepository();

        var projectRepository =
            new FakeProjectRepository();

        var projectMemberRepository =
            new FakeProjectMemberRepository();

        var unitOfWork =
            new FakeUnitOfWork();

        var handler =
            new UpdateProjectHandler(
                tenantRepository,
                userRepository,
                projectRepository,
                projectMemberRepository,
                unitOfWork);

        var result =
            await handler.HandleAsync(
                new UpdateProjectCommand(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    "Projeto",
                    null,
                    null));

        Assert.True(result.IsFailure);

        Assert.Equal(
            TenantErrors.NotFound,
            result.Error);

        Assert.Equal(
            0,
            unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldReturnFailure_WhenTenantIsInactive()
    {
        var fixture =
            CreateFixture(
                UserRole.TenantAdmin);

        fixture.Tenant.Deactivate();

        var result =
            await fixture.Handler.HandleAsync(
                CreateCommand(fixture));

        Assert.True(result.IsFailure);

        Assert.Equal(
            TenantErrors.Inactive,
            result.Error);

        Assert.Equal(
            0,
            fixture.UnitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldReturnFailure_WhenRequesterDoesNotExist()
    {
        var fixture =
            CreateFixture(
                UserRole.TenantAdmin);

        fixture.UserRepository.UserToReturn =
            null;

        var result =
            await fixture.Handler.HandleAsync(
                CreateCommand(fixture));

        Assert.True(result.IsFailure);

        Assert.Equal(
            UserErrors.NotFound,
            result.Error);

        Assert.Equal(
            0,
            fixture.UnitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldReturnFailure_WhenRequesterIsInactive()
    {
        var fixture =
            CreateFixture(
                UserRole.TenantAdmin);

        fixture.Requester.Deactivate();

        var result =
            await fixture.Handler.HandleAsync(
                CreateCommand(fixture));

        Assert.True(result.IsFailure);

        Assert.Equal(
            UserErrors.Inactive,
            result.Error);

        Assert.Equal(
            0,
            fixture.UnitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldReturnFailure_WhenProjectDoesNotExist()
    {
        var fixture =
            CreateFixture(
                UserRole.TenantAdmin);

        fixture.ProjectRepository.ProjectToReturn =
            null;

        var result =
            await fixture.Handler.HandleAsync(
                CreateCommand(fixture));

        Assert.True(result.IsFailure);

        Assert.Equal(
            ProjectErrors.NotFound,
            result.Error);

        Assert.Equal(
            0,
            fixture.UnitOfWork.SaveChangesCallCount);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public async Task
    HandleAsync_ShouldThrow_WhenRequiredPublicIdIsEmpty(
        int publicIdToClear)
    {
        var fixture =
            CreateFixture(
                UserRole.TenantAdmin);

        var command =
            new UpdateProjectCommand(
                publicIdToClear == 1
                    ? Guid.Empty
                    : fixture.Tenant.PublicId,
                publicIdToClear == 2
                    ? Guid.Empty
                    : fixture.Project.PublicId,
                publicIdToClear == 3
                    ? Guid.Empty
                    : fixture.Requester.PublicId,
                "Projeto atualizado",
                null,
                null);

        await Assert.ThrowsAsync<ArgumentException>(
            () => fixture.Handler.HandleAsync(
                command));

        Assert.Equal(
            0,
            fixture.UnitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldThrow_WhenProjectNameIsEmpty()
    {
        var fixture =
            CreateFixture(
                UserRole.TenantAdmin);

        var originalName =
            fixture.Project.Name;

        var command =
            new UpdateProjectCommand(
                fixture.Tenant.PublicId,
                fixture.Project.PublicId,
                fixture.Requester.PublicId,
                "   ",
                "Nova descrição",
                null);

        var exception =
            await Assert.ThrowsAsync<ArgumentException>(
                () => fixture.Handler.HandleAsync(
                    command));

        Assert.Equal(
            "name",
            exception.ParamName);

        Assert.Equal(
            originalName,
            fixture.Project.Name);

        Assert.Equal(
            0,
            fixture.UnitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldReturnResponsiblePublicId_WhenProjectHasResponsibleUser()
    {
        var tenant =
            CreatePersistedTenant();

        var requester =
            CreatePersistedUser(
                tenant.Id,
                10,
                UserRole.TenantAdmin);

        var creator =
            CreatePersistedUser(
                tenant.Id,
                20,
                UserRole.ProjectManager);

        var responsible =
            CreatePersistedUser(
                tenant.Id,
                30,
                UserRole.Member);

        var project =
            new Project(
                tenant.Id,
                "Projeto",
                creator.Id,
                "Descrição",
                responsible.Id);

        EntityTestHelper.SetId(
            project,
            100);

        var tenantRepository =
            new FakeTenantRepository
            {
                TenantToReturn = tenant
            };

        var userRepository =
            new FakeUserRepository
            {
                UserToReturn = requester
            };

        userRepository.UsersByIdToReturn.Add(
            creator.Id,
            creator);

        userRepository.UsersByIdToReturn.Add(
            responsible.Id,
            responsible);

        var projectRepository =
            new FakeProjectRepository
            {
                ProjectToReturn = project
            };

        var unitOfWork =
            new FakeUnitOfWork();

        var handler =
            new UpdateProjectHandler(
                tenantRepository,
                userRepository,
                projectRepository,
                new FakeProjectMemberRepository(),
                unitOfWork);

        var result =
            await handler.HandleAsync(
                new UpdateProjectCommand(
                    tenant.PublicId,
                    project.PublicId,
                    requester.PublicId,
                    "Projeto atualizado",
                    null,
                    null));

        Assert.True(result.IsSuccess);

        var response =
            Assert.IsType<UpdateProjectResult>(
                result.Value);

        Assert.Equal(
            responsible.PublicId,
            response.ResponsibleUserPublicId);

        Assert.Equal(
            1,
            unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldThrow_WhenCreatedByUserDoesNotExist()
    {
        var fixture =
            CreateFixture(
                UserRole.TenantAdmin);

        fixture.UserRepository
            .UsersByIdToReturn
            .Remove(
                fixture.Creator.Id);

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => fixture.Handler.HandleAsync(
                    CreateCommand(fixture)));

        Assert.Equal(
            "O usuário criador associado ao projeto não foi encontrado.",
            exception.Message);

        Assert.Equal(
            0,
            fixture.UnitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldThrow_WhenResponsibleUserDoesNotExist()
    {
        var tenant =
            CreatePersistedTenant();

        var requester =
            CreatePersistedUser(
                tenant.Id,
                10,
                UserRole.TenantAdmin);

        var creator =
            CreatePersistedUser(
                tenant.Id,
                20,
                UserRole.ProjectManager);

        var project =
            new Project(
                tenant.Id,
                "Projeto",
                creator.Id,
                responsibleUserId: 30);

        EntityTestHelper.SetId(
            project,
            100);

        var tenantRepository =
            new FakeTenantRepository
            {
                TenantToReturn = tenant
            };

        var userRepository =
            new FakeUserRepository
            {
                UserToReturn = requester
            };

        userRepository.UsersByIdToReturn.Add(
            creator.Id,
            creator);

        var projectRepository =
            new FakeProjectRepository
            {
                ProjectToReturn = project
            };

        var unitOfWork =
            new FakeUnitOfWork();

        var handler =
            new UpdateProjectHandler(
                tenantRepository,
                userRepository,
                projectRepository,
                new FakeProjectMemberRepository(),
                unitOfWork);

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => handler.HandleAsync(
                    new UpdateProjectCommand(
                        tenant.PublicId,
                        project.PublicId,
                        requester.PublicId,
                        "Projeto atualizado",
                        null,
                        null)));

        Assert.Equal(
            "O usuário responsável associado ao projeto não foi encontrado.",
            exception.Message);

        Assert.Equal(
            0,
            unitOfWork.SaveChangesCallCount);
    }

    private static Fixture CreateFixture(
        UserRole requesterRole)
    {
        var tenant =
            CreatePersistedTenant();

        var requester =
            CreatePersistedUser(
                tenant.Id,
                10,
                requesterRole);

        var creator =
            CreatePersistedUser(
                tenant.Id,
                20,
                UserRole.TenantAdmin);

        var project =
            CreatePersistedProject(
                tenant.Id,
                creator.Id);

        var tenantRepository =
            new FakeTenantRepository
            {
                TenantToReturn = tenant
            };

        var userRepository =
            new FakeUserRepository
            {
                UserToReturn = requester
            };

        userRepository.UsersByIdToReturn.Add(
            creator.Id,
            creator);

        var projectRepository =
            new FakeProjectRepository
            {
                ProjectToReturn = project
            };

        var projectMemberRepository =
            new FakeProjectMemberRepository
            {
                IsActiveMemberResult = true
            };

        var unitOfWork =
            new FakeUnitOfWork();

        var handler =
            new UpdateProjectHandler(
                tenantRepository,
                userRepository,
                projectRepository,
                projectMemberRepository,
                unitOfWork);

        return new Fixture(
            tenant,
            requester,
            creator,
            project,
            userRepository,
            projectRepository,
            projectMemberRepository,
            unitOfWork,
            handler);
    }

    private static UpdateProjectCommand CreateCommand(
        Fixture fixture)
    {
        return new UpdateProjectCommand(
            fixture.Tenant.PublicId,
            fixture.Project.PublicId,
            fixture.Requester.PublicId,
            "Projeto atualizado",
            "Descrição atualizada",
            new DateTime(
                2026,
                12,
                31,
                12,
                0,
                0,
                DateTimeKind.Utc));
    }

    private static Tenant CreatePersistedTenant()
    {
        var tenant =
            new Tenant(
                "Empresa de Teste",
                "REG-UPDATE-PROJECT",
                "empresa-update-project@test.local");

        EntityTestHelper.SetId(
            tenant,
            42);

        return tenant;
    }

    private static User CreatePersistedUser(
        long tenantId,
        long id,
        UserRole role)
    {
        var user =
            new User(
                tenantId,
                $"Usuário {id}",
                $"usuario{id}@update-project.test",
                "password-hash",
                role);

        EntityTestHelper.SetId(
            user,
            id);

        return user;
    }

    private static Project CreatePersistedProject(
        long tenantId,
        long createdByUserId)
    {
        var project =
            new Project(
                tenantId,
                "Projeto original",
                createdByUserId,
                "Descrição original",
                dueDate: new DateTime(
                    2026,
                    10,
                    31,
                    12,
                    0,
                    0,
                    DateTimeKind.Utc));

        EntityTestHelper.SetId(
            project,
            100);

        return project;
    }

    private sealed record Fixture(
        Tenant Tenant,
        User Requester,
        User Creator,
        Project Project,
        FakeUserRepository UserRepository,
        FakeProjectRepository ProjectRepository,
        FakeProjectMemberRepository ProjectMemberRepository,
        FakeUnitOfWork UnitOfWork,
        UpdateProjectHandler Handler);
}