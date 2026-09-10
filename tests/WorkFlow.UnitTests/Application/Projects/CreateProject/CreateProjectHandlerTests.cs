using WorkFlow.Application.Projects;
using WorkFlow.Application.Projects.CreateProject;
using WorkFlow.Application.Tenants;
using WorkFlow.Application.Users;
using WorkFlow.Domain.Entities;
using WorkFlow.Domain.Enums;
using WorkFlow.UnitTests.Application.Projects.Fakes;
using WorkFlow.UnitTests.Application.Tenants.Fakes;
using WorkFlow.UnitTests.Application.Users.Fakes;
using WorkFlow.UnitTests.Common;

namespace WorkFlow.UnitTests.Application.Projects.CreateProject;

public class CreateProjectHandlerTests
{
    [Fact]
    public async Task
    HandleAsync_ShouldCreateProjectAndAddCreatorAsMember_WhenDataIsValid()
    {
        var tenant =
            CreatePersistedTenant();

        var creator =
            CreatePersistedUser(
                tenant.Id,
                UserRole.TenantAdmin);

        var tenantRepository =
            new FakeTenantRepository
            {
                TenantToReturn = tenant
            };

        var userRepository =
            new FakeUserRepository
            {
                UserToReturn = creator
            };

        var projectRepository =
            new FakeProjectRepository();

        var projectMemberRepository =
            new FakeProjectMemberRepository();

        var projectMemberPermissionRepository =
            new FakeProjectMemberPermissionRepository();

        var unitOfWork =
            new FakeUnitOfWork();

        unitOfWork.OnSaveChanges =
            saveChangesCallCount =>
            {
                if (saveChangesCallCount == 1)
                {
                    var project =
                        Assert.IsType<Project>(
                            projectRepository.AddedProject);

                    EntityTestHelper.SetId(
                        project,
                        100);
                }
            };

        var handler =
            new CreateProjectHandler(
                tenantRepository,
                userRepository,
                projectRepository,
                projectMemberRepository,
                projectMemberPermissionRepository,
                unitOfWork);

        var dueDate =
            new DateTime(
                2026,
                9,
                30,
                12,
                0,
                0,
                DateTimeKind.Utc);

        var command =
            new CreateProjectCommand(
                tenant.PublicId,
                creator.PublicId,
                "Projeto de Teste",
                "Descrição do projeto",
                dueDate);

        var result =
            await handler.HandleAsync(command);

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Null(result.Error);

        var response =
            Assert.IsType<CreateProjectResult>(
                result.Value);

        Assert.Equal(
            tenant.PublicId,
            tenantRepository.CheckedPublicId);

        Assert.Equal(
            tenant.Id,
            userRepository.CheckedGetByPublicIdTenantId);

        Assert.Equal(
            creator.PublicId,
            userRepository.CheckedPublicId);

        var addedProject =
            Assert.IsType<Project>(
                projectRepository.AddedProject);

        Assert.Equal(
            100,
            addedProject.Id);

        Assert.Equal(
            tenant.Id,
            addedProject.TenantId);

        Assert.Equal(
            creator.Id,
            addedProject.CreatedByUserId);

        Assert.Equal(
            command.Name,
            addedProject.Name);

        Assert.Equal(
            command.Description,
            addedProject.Description);

        Assert.Equal(
            ProjectStatus.Planning,
            addedProject.Status);

        Assert.Equal(
            dueDate,
            addedProject.DueDate);

        Assert.Null(
            addedProject.ResponsibleUserId);

        var addedMember =
            Assert.IsType<ProjectMember>(
                projectMemberRepository.AddedProjectMember);

        Assert.Equal(
            addedProject.Id,
            addedMember.ProjectId);

        Assert.Equal(
            creator.Id,
            addedMember.UserId);

        Assert.Equal(
            creator.Id,
            addedMember.AddedByUserId);

        Assert.True(
            addedMember.IsActive);

        Assert.Empty(
            projectMemberPermissionRepository
                .AddedProjectMemberPermissions);

        Assert.Equal(
            addedProject.PublicId,
            response.PublicId);

        Assert.Equal(
            tenant.PublicId,
            response.TenantPublicId);

        Assert.Equal(
            creator.PublicId,
            response.CreatedByUserPublicId);

        Assert.Equal(
            addedProject.Name,
            response.Name);

        Assert.Equal(
            addedProject.Description,
            response.Description);

        Assert.Equal(
            addedProject.Status,
            response.Status);

        Assert.Equal(
            addedProject.DueDate,
            response.DueDate);

        Assert.Equal(
            addedProject.CreatedAt,
            response.CreatedAt);

        Assert.Equal(
            1,
            unitOfWork.BeginTransactionCallCount);

        Assert.Equal(
            2,
            unitOfWork.SaveChangesCallCount);

        Assert.Equal(
            1,
            unitOfWork.CommitCallCount);

        Assert.Equal(
            0,
            unitOfWork.RollbackCallCount);
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
            new CreateProjectHandler(
                tenantRepository,
                userRepository,
                projectRepository,
                projectMemberRepository,
                new FakeProjectMemberPermissionRepository(),
                unitOfWork);

        var command =
            new CreateProjectCommand(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "Projeto de Teste",
                "Descrição do projeto",
                null);

        var result =
            await handler.HandleAsync(command);

        Assert.True(result.IsFailure);
        Assert.False(result.IsSuccess);
        Assert.Null(result.Value);

        Assert.Equal(
            TenantErrors.NotFound,
            result.Error);

        Assert.Null(
            userRepository.CheckedGetByPublicIdTenantId);

        Assert.Null(
            projectRepository.AddedProject);

        Assert.Null(
            projectMemberRepository.AddedProjectMember);

        Assert.Equal(
            0,
            unitOfWork.BeginTransactionCallCount);

        Assert.Equal(
            0,
            unitOfWork.SaveChangesCallCount);

        Assert.Equal(
            0,
            unitOfWork.CommitCallCount);

        Assert.Equal(
            0,
            unitOfWork.RollbackCallCount);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldReturnFailure_WhenTenantIsInactive()
    {
        var tenant =
            CreatePersistedTenant();

        tenant.Deactivate();

        var tenantRepository =
            new FakeTenantRepository
            {
                TenantToReturn = tenant
            };

        var userRepository =
            new FakeUserRepository();

        var projectRepository =
            new FakeProjectRepository();

        var projectMemberRepository =
            new FakeProjectMemberRepository();

        var unitOfWork =
            new FakeUnitOfWork();

        var handler =
            new CreateProjectHandler(
                tenantRepository,
                userRepository,
                projectRepository,
                projectMemberRepository,
                new FakeProjectMemberPermissionRepository(),
                unitOfWork);

        var command =
            new CreateProjectCommand(
                tenant.PublicId,
                Guid.NewGuid(),
                "Projeto de Teste",
                "Descrição do projeto",
                null);

        var result =
            await handler.HandleAsync(command);

        Assert.True(result.IsFailure);
        Assert.False(result.IsSuccess);
        Assert.Null(result.Value);

        Assert.Equal(
            TenantErrors.Inactive,
            result.Error);

        Assert.Null(
            userRepository.CheckedGetByPublicIdTenantId);

        Assert.Null(
            projectRepository.AddedProject);

        Assert.Null(
            projectMemberRepository.AddedProjectMember);

        Assert.Equal(
            0,
            unitOfWork.BeginTransactionCallCount);

        Assert.Equal(
            0,
            unitOfWork.SaveChangesCallCount);

        Assert.Equal(
            0,
            unitOfWork.CommitCallCount);

        Assert.Equal(
            0,
            unitOfWork.RollbackCallCount);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldReturnFailure_WhenCreatorDoesNotExist()
    {
        var tenant =
            CreatePersistedTenant();

        var tenantRepository =
            new FakeTenantRepository
            {
                TenantToReturn = tenant
            };

        var userRepository =
            new FakeUserRepository();

        var projectRepository =
            new FakeProjectRepository();

        var projectMemberRepository =
            new FakeProjectMemberRepository();

        var unitOfWork =
            new FakeUnitOfWork();

        var creatorPublicId =
            Guid.NewGuid();

        var handler =
            new CreateProjectHandler(
                tenantRepository,
                userRepository,
                projectRepository,
                projectMemberRepository,
                new FakeProjectMemberPermissionRepository(),
                unitOfWork);

        var command =
            new CreateProjectCommand(
                tenant.PublicId,
                creatorPublicId,
                "Projeto de Teste",
                "Descrição do projeto",
                null);

        var result =
            await handler.HandleAsync(command);

        Assert.True(result.IsFailure);
        Assert.False(result.IsSuccess);
        Assert.Null(result.Value);

        Assert.Equal(
            UserErrors.NotFound,
            result.Error);

        Assert.Equal(
            tenant.Id,
            userRepository.CheckedGetByPublicIdTenantId);

        Assert.Equal(
            creatorPublicId,
            userRepository.CheckedPublicId);

        Assert.Null(
            projectRepository.AddedProject);

        Assert.Null(
            projectMemberRepository.AddedProjectMember);

        Assert.Equal(
            0,
            unitOfWork.BeginTransactionCallCount);

        Assert.Equal(
            0,
            unitOfWork.SaveChangesCallCount);

        Assert.Equal(
            0,
            unitOfWork.CommitCallCount);

        Assert.Equal(
            0,
            unitOfWork.RollbackCallCount);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldReturnFailure_WhenCreatorIsInactive()
    {
        var tenant =
            CreatePersistedTenant();

        var creator =
            CreatePersistedUser(
                tenant.Id,
                UserRole.TenantAdmin);

        creator.Deactivate();

        var tenantRepository =
            new FakeTenantRepository
            {
                TenantToReturn = tenant
            };

        var userRepository =
            new FakeUserRepository
            {
                UserToReturn = creator
            };

        var projectRepository =
            new FakeProjectRepository();

        var projectMemberRepository =
            new FakeProjectMemberRepository();

        var unitOfWork =
            new FakeUnitOfWork();

        var handler =
            new CreateProjectHandler(
                tenantRepository,
                userRepository,
                projectRepository,
                projectMemberRepository,
                new FakeProjectMemberPermissionRepository(),
                unitOfWork);

        var command =
            new CreateProjectCommand(
                tenant.PublicId,
                creator.PublicId,
                "Projeto de Teste",
                "Descrição do projeto",
                null);

        var result =
            await handler.HandleAsync(command);

        Assert.True(result.IsFailure);
        Assert.False(result.IsSuccess);
        Assert.Null(result.Value);

        Assert.Equal(
            UserErrors.Inactive,
            result.Error);

        Assert.Equal(
            tenant.Id,
            userRepository.CheckedGetByPublicIdTenantId);

        Assert.Equal(
            creator.PublicId,
            userRepository.CheckedPublicId);

        Assert.Null(
            projectRepository.AddedProject);

        Assert.Null(
            projectMemberRepository.AddedProjectMember);

        Assert.Equal(
            0,
            unitOfWork.BeginTransactionCallCount);

        Assert.Equal(
            0,
            unitOfWork.SaveChangesCallCount);

        Assert.Equal(
            0,
            unitOfWork.CommitCallCount);

        Assert.Equal(
            0,
            unitOfWork.RollbackCallCount);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldReturnFailure_WhenCreatorRoleCannotCreateProject()
    {
        var tenant =
            CreatePersistedTenant();

        var creator =
            CreatePersistedUser(
                tenant.Id,
                UserRole.Member);

        var tenantRepository =
            new FakeTenantRepository
            {
                TenantToReturn = tenant
            };

        var userRepository =
            new FakeUserRepository
            {
                UserToReturn = creator
            };

        var projectRepository =
            new FakeProjectRepository();

        var projectMemberRepository =
            new FakeProjectMemberRepository();

        var unitOfWork =
            new FakeUnitOfWork();

        var handler =
            new CreateProjectHandler(
                tenantRepository,
                userRepository,
                projectRepository,
                projectMemberRepository,
                new FakeProjectMemberPermissionRepository(),
                unitOfWork);

        var command =
            new CreateProjectCommand(
                tenant.PublicId,
                creator.PublicId,
                "Projeto de Teste",
                "Descrição do projeto",
                null);

        var result =
            await handler.HandleAsync(command);

        Assert.True(result.IsFailure);
        Assert.False(result.IsSuccess);
        Assert.Null(result.Value);

        Assert.Equal(
            ProjectErrors.CreationNotAllowed,
            result.Error);

        Assert.Equal(
            tenant.Id,
            userRepository.CheckedGetByPublicIdTenantId);

        Assert.Equal(
            creator.PublicId,
            userRepository.CheckedPublicId);

        Assert.Null(
            projectRepository.AddedProject);

        Assert.Null(
            projectMemberRepository.AddedProjectMember);

        Assert.Equal(
            0,
            unitOfWork.BeginTransactionCallCount);

        Assert.Equal(
            0,
            unitOfWork.SaveChangesCallCount);

        Assert.Equal(
            0,
            unitOfWork.CommitCallCount);

        Assert.Equal(
            0,
            unitOfWork.RollbackCallCount);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldCreateProjectAndDefaultPermissions_WhenCreatorIsProjectManager()
    {
        var tenant =
            CreatePersistedTenant();

        var creator =
            CreatePersistedUser(
                tenant.Id,
                UserRole.ProjectManager);

        var tenantRepository =
            new FakeTenantRepository
            {
                TenantToReturn = tenant
            };

        var userRepository =
            new FakeUserRepository
            {
                UserToReturn = creator
            };

        var projectRepository =
            new FakeProjectRepository();

        var projectMemberRepository =
            new FakeProjectMemberRepository();

        var projectMemberPermissionRepository =
            new FakeProjectMemberPermissionRepository();

        var unitOfWork =
            new FakeUnitOfWork();

        unitOfWork.OnSaveChanges =
            saveChangesCallCount =>
            {
                if (saveChangesCallCount == 1)
                {
                    var project =
                        Assert.IsType<Project>(
                            projectRepository.AddedProject);

                    EntityTestHelper.SetId(
                        project,
                        100);

                    return;
                }

                if (saveChangesCallCount == 2)
                {
                    var projectMember =
                        Assert.IsType<ProjectMember>(
                            projectMemberRepository
                                .AddedProjectMember);

                    EntityTestHelper.SetId(
                        projectMember,
                        200);
                }
            };

        var handler =
            new CreateProjectHandler(
                tenantRepository,
                userRepository,
                projectRepository,
                projectMemberRepository,
                projectMemberPermissionRepository,
                unitOfWork);

        var command =
            new CreateProjectCommand(
                tenant.PublicId,
                creator.PublicId,
                "Projeto do Gerente",
                null,
                null);

        var result =
            await handler.HandleAsync(
                command);

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Null(result.Error);

        var addedProject =
            Assert.IsType<Project>(
                projectRepository.AddedProject);

        var addedMember =
            Assert.IsType<ProjectMember>(
                projectMemberRepository.AddedProjectMember);

        Assert.Equal(
            creator.Id,
            addedProject.CreatedByUserId);

        Assert.Equal(
            creator.Id,
            addedMember.UserId);

        Assert.Equal(
            creator.Id,
            addedMember.AddedByUserId);

        Assert.Equal(
            200,
            addedMember.Id);

        Assert.Equal(
            3,
            projectMemberPermissionRepository
                .AddedProjectMemberPermissions.Count);

        Assert.Contains(
            projectMemberPermissionRepository
                .AddedProjectMemberPermissions,
            permission =>
                permission.ProjectMemberId ==
                    addedMember.Id &&
                permission.Permission ==
                    ProjectPermission.EditProject &&
                permission.GrantedByUserId ==
                    creator.Id);

        Assert.Contains(
            projectMemberPermissionRepository
                .AddedProjectMemberPermissions,
            permission =>
                permission.ProjectMemberId ==
                    addedMember.Id &&
                permission.Permission ==
                    ProjectPermission.ManageProjectMembers &&
                permission.GrantedByUserId ==
                    creator.Id);

        Assert.Contains(
            projectMemberPermissionRepository
                .AddedProjectMemberPermissions,
            permission =>
                permission.ProjectMemberId ==
                    addedMember.Id &&
                permission.Permission ==
                    ProjectPermission.ManageProjectPermissions &&
                permission.GrantedByUserId ==
                    creator.Id);

        Assert.All(
            projectMemberPermissionRepository
                .AddedProjectMemberPermissions,
            permission =>
                Assert.True(
                    permission.IsActive));

        Assert.Equal(
            1,
            unitOfWork.BeginTransactionCallCount);

        Assert.Equal(
            3,
            unitOfWork.SaveChangesCallCount);

        Assert.Equal(
            1,
            unitOfWork.CommitCallCount);

        Assert.Equal(
            0,
            unitOfWork.RollbackCallCount);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldRollbackTransaction_WhenSavingProjectMemberFails()
    {
        var tenant =
            CreatePersistedTenant();

        var creator =
            CreatePersistedUser(
                tenant.Id,
                UserRole.TenantAdmin);

        var tenantRepository =
            new FakeTenantRepository
            {
                TenantToReturn = tenant
            };

        var userRepository =
            new FakeUserRepository
            {
                UserToReturn = creator
            };

        var projectRepository =
            new FakeProjectRepository();

        var projectMemberRepository =
            new FakeProjectMemberRepository();

        var unitOfWork =
            new FakeUnitOfWork();

        unitOfWork.OnSaveChanges =
            saveChangesCallCount =>
            {
                if (saveChangesCallCount == 1)
                {
                    var project =
                        Assert.IsType<Project>(
                            projectRepository.AddedProject);

                    EntityTestHelper.SetId(
                        project,
                        100);

                    return;
                }

                if (saveChangesCallCount == 2)
                {
                    throw new InvalidOperationException(
                        "Falha simulada ao salvar o membro.");
                }
            };

        var handler =
            new CreateProjectHandler(
                tenantRepository,
                userRepository,
                projectRepository,
                projectMemberRepository,
                new FakeProjectMemberPermissionRepository(),
                unitOfWork);

        var command =
            new CreateProjectCommand(
                tenant.PublicId,
                creator.PublicId,
                "Projeto de Teste",
                null,
                null);

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => handler.HandleAsync(command));

        Assert.Equal(
            "Falha simulada ao salvar o membro.",
            exception.Message);

        Assert.NotNull(
            projectRepository.AddedProject);

        Assert.NotNull(
            projectMemberRepository.AddedProjectMember);

        Assert.Equal(
            1,
            unitOfWork.BeginTransactionCallCount);

        Assert.Equal(
            2,
            unitOfWork.SaveChangesCallCount);

        Assert.Equal(
            0,
            unitOfWork.CommitCallCount);

        Assert.Equal(
            1,
            unitOfWork.RollbackCallCount);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldThrow_WhenTenantPublicIdIsEmpty()
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
            new CreateProjectHandler(
                tenantRepository,
                userRepository,
                projectRepository,
                projectMemberRepository,
                new FakeProjectMemberPermissionRepository(),
                unitOfWork);

        var command =
            new CreateProjectCommand(
                Guid.Empty,
                Guid.NewGuid(),
                "Projeto de Teste",
                null,
                null);

        var exception =
            await Assert.ThrowsAsync<ArgumentException>(
                () => handler.HandleAsync(command));

        Assert.Equal(
            "TenantPublicId",
            exception.ParamName);

        Assert.Null(
            tenantRepository.CheckedPublicId);

        Assert.Null(
            userRepository.CheckedGetByPublicIdTenantId);

        Assert.Null(
            projectRepository.AddedProject);

        Assert.Null(
            projectMemberRepository.AddedProjectMember);

        Assert.Equal(
            0,
            unitOfWork.BeginTransactionCallCount);

        Assert.Equal(
            0,
            unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldThrow_WhenCreatedByUserPublicIdIsEmpty()
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
            new CreateProjectHandler(
                tenantRepository,
                userRepository,
                projectRepository,
                projectMemberRepository,
                new FakeProjectMemberPermissionRepository(),
                unitOfWork);

        var command =
            new CreateProjectCommand(
                Guid.NewGuid(),
                Guid.Empty,
                "Projeto de Teste",
                null,
                null);

        var exception =
            await Assert.ThrowsAsync<ArgumentException>(
                () => handler.HandleAsync(command));

        Assert.Equal(
            "CreatedByUserPublicId",
            exception.ParamName);

        Assert.Null(
            tenantRepository.CheckedPublicId);

        Assert.Null(
            userRepository.CheckedGetByPublicIdTenantId);

        Assert.Null(
            projectRepository.AddedProject);

        Assert.Null(
            projectMemberRepository.AddedProjectMember);

        Assert.Equal(
            0,
            unitOfWork.BeginTransactionCallCount);

        Assert.Equal(
            0,
            unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldThrow_WhenProjectNameIsEmpty()
    {
        var tenant =
            CreatePersistedTenant();

        var creator =
            CreatePersistedUser(
                tenant.Id,
                UserRole.TenantAdmin);

        var tenantRepository =
            new FakeTenantRepository
            {
                TenantToReturn = tenant
            };

        var userRepository =
            new FakeUserRepository
            {
                UserToReturn = creator
            };

        var projectRepository =
            new FakeProjectRepository();

        var projectMemberRepository =
            new FakeProjectMemberRepository();

        var unitOfWork =
            new FakeUnitOfWork();

        var handler =
            new CreateProjectHandler(
                tenantRepository,
                userRepository,
                projectRepository,
                projectMemberRepository,
                new FakeProjectMemberPermissionRepository(),
                unitOfWork);

        var command =
            new CreateProjectCommand(
                tenant.PublicId,
                creator.PublicId,
                "   ",
                null,
                null);

        var exception =
            await Assert.ThrowsAsync<ArgumentException>(
                () => handler.HandleAsync(command));

        Assert.Equal(
            "name",
            exception.ParamName);

        Assert.Null(
            projectRepository.AddedProject);

        Assert.Null(
            projectMemberRepository.AddedProjectMember);

        Assert.Equal(
            0,
            unitOfWork.BeginTransactionCallCount);

        Assert.Equal(
            0,
            unitOfWork.SaveChangesCallCount);

        Assert.Equal(
            0,
            unitOfWork.CommitCallCount);

        Assert.Equal(
            0,
            unitOfWork.RollbackCallCount);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldRollbackTransaction_WhenSavingProjectFails()
    {
        var tenant =
            CreatePersistedTenant();

        var creator =
            CreatePersistedUser(
                tenant.Id,
                UserRole.TenantAdmin);

        var tenantRepository =
            new FakeTenantRepository
            {
                TenantToReturn = tenant
            };

        var userRepository =
            new FakeUserRepository
            {
                UserToReturn = creator
            };

        var projectRepository =
            new FakeProjectRepository();

        var projectMemberRepository =
            new FakeProjectMemberRepository();

        var unitOfWork =
            new FakeUnitOfWork
            {
                OnSaveChanges =
                    _ => throw new InvalidOperationException(
                        "Falha simulada ao salvar o projeto.")
            };

        var handler =
            new CreateProjectHandler(
                tenantRepository,
                userRepository,
                projectRepository,
                projectMemberRepository,
                new FakeProjectMemberPermissionRepository(),
                unitOfWork);

        var command =
            new CreateProjectCommand(
                tenant.PublicId,
                creator.PublicId,
                "Projeto de Teste",
                null,
                null);

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => handler.HandleAsync(command));

        Assert.Equal(
            "Falha simulada ao salvar o projeto.",
            exception.Message);

        Assert.NotNull(
            projectRepository.AddedProject);

        Assert.Null(
            projectMemberRepository.AddedProjectMember);

        Assert.Equal(
            1,
            unitOfWork.BeginTransactionCallCount);

        Assert.Equal(
            1,
            unitOfWork.SaveChangesCallCount);

        Assert.Equal(
            0,
            unitOfWork.CommitCallCount);

        Assert.Equal(
            1,
            unitOfWork.RollbackCallCount);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldRollbackTransaction_WhenSavingDefaultPermissionsFails()
    {
        var tenant =
            CreatePersistedTenant();

        var creator =
            CreatePersistedUser(
                tenant.Id,
                UserRole.ProjectManager);

        var tenantRepository =
            new FakeTenantRepository
            {
                TenantToReturn = tenant
            };

        var userRepository =
            new FakeUserRepository
            {
                UserToReturn = creator
            };

        var projectRepository =
            new FakeProjectRepository();

        var projectMemberRepository =
            new FakeProjectMemberRepository();

        var projectMemberPermissionRepository =
            new FakeProjectMemberPermissionRepository();

        var unitOfWork =
            new FakeUnitOfWork();

        unitOfWork.OnSaveChanges =
            saveChangesCallCount =>
            {
                if (saveChangesCallCount == 1)
                {
                    var project =
                        Assert.IsType<Project>(
                            projectRepository.AddedProject);

                    EntityTestHelper.SetId(
                        project,
                        100);

                    return;
                }

                if (saveChangesCallCount == 2)
                {
                    var projectMember =
                        Assert.IsType<ProjectMember>(
                            projectMemberRepository
                                .AddedProjectMember);

                    EntityTestHelper.SetId(
                        projectMember,
                        200);

                    return;
                }

                if (saveChangesCallCount == 3)
                {
                    throw new InvalidOperationException(
                        "Falha simulada ao salvar as permissões.");
                }
            };

        var handler =
            new CreateProjectHandler(
                tenantRepository,
                userRepository,
                projectRepository,
                projectMemberRepository,
                projectMemberPermissionRepository,
                unitOfWork);

        var command =
            new CreateProjectCommand(
                tenant.PublicId,
                creator.PublicId,
                "Projeto do Gerente",
                null,
                null);

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () =>
                    handler.HandleAsync(
                        command));

        Assert.Equal(
            "Falha simulada ao salvar as permissões.",
            exception.Message);

        Assert.Equal(
            3,
            projectMemberPermissionRepository
                .AddedProjectMemberPermissions.Count);

        Assert.Equal(
            1,
            unitOfWork.BeginTransactionCallCount);

        Assert.Equal(
            3,
            unitOfWork.SaveChangesCallCount);

        Assert.Equal(
            0,
            unitOfWork.CommitCallCount);

        Assert.Equal(
            1,
            unitOfWork.RollbackCallCount);
    }

    private static Tenant CreatePersistedTenant()
    {
        var tenant =
            new Tenant(
                "Empresa de Teste",
                "REG-CREATE-PROJECT",
                "empresa@test.local");

        EntityTestHelper.SetId(
            tenant,
            42);

        return tenant;
    }

    private static User CreatePersistedUser(
        long tenantId,
        UserRole role)
    {
        var user =
            new User(
                tenantId,
                "Usuário Criador",
                "criador@test.local",
                "password-hash",
                role);

        EntityTestHelper.SetId(
            user,
            84);

        return user;
    }
}