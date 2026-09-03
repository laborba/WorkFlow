using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WorkFlow.API.Contracts.Projects;
using WorkFlow.API.Controllers;
using WorkFlow.Application.Projects.CreateProject;
using WorkFlow.Domain.Entities;
using WorkFlow.Domain.Enums;
using WorkFlow.UnitTests.Application.Projects.Fakes;
using WorkFlow.UnitTests.Application.Tenants.Fakes;
using WorkFlow.UnitTests.Application.Users.Fakes;
using WorkFlow.UnitTests.Common;

namespace WorkFlow.UnitTests.API.Projects;

public sealed class ProjectsControllerTests
{
    [Fact]
    public async Task
    Create_ShouldUseAuthenticatedUserAsCreator_WhenDataIsValid()
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
                }
            };

        var handler =
            new CreateProjectHandler(
                tenantRepository,
                userRepository,
                projectRepository,
                projectMemberRepository,
                unitOfWork);

        var controller =
            new ProjectsController(
                handler);

        var identity =
            new ClaimsIdentity(
                new[]
                {
                    new Claim(
                        ClaimTypes.NameIdentifier,
                        creator.PublicId.ToString())
                },
                "Test");

        controller.ControllerContext =
            new ControllerContext
            {
                HttpContext =
                    new DefaultHttpContext
                    {
                        User =
                            new ClaimsPrincipal(
                                identity)
                    }
            };

        var dueDate =
            new DateTime(
                2026,
                9,
                30,
                12,
                0,
                0,
                DateTimeKind.Utc);

        var request =
            new CreateProjectRequest(
                "Projeto de Teste",
                "Descrição do projeto",
                dueDate);

        var result =
            await controller.Create(
                tenant.PublicId,
                request,
                CancellationToken.None);

        var createdResult =
            Assert.IsType<CreatedResult>(
                result.Result);

        var response =
            Assert.IsType<CreateProjectResponse>(
                createdResult.Value);

        Assert.Equal(
            creator.PublicId,
            userRepository.CheckedPublicId);

        Assert.Equal(
            tenant.Id,
            userRepository.CheckedGetByPublicIdTenantId);

        var addedProject =
            Assert.IsType<Project>(
                projectRepository.AddedProject);

        Assert.Equal(
            creator.Id,
            addedProject.CreatedByUserId);

        Assert.Equal(
            creator.PublicId,
            response.CreatedByUserPublicId);

        Assert.Equal(
            tenant.PublicId,
            response.TenantPublicId);

        Assert.Equal(
            request.Name,
            response.Name);

        Assert.Equal(
            request.Description,
            response.Description);

        Assert.Equal(
            dueDate,
            response.DueDate);

        Assert.Equal(
            ProjectStatus.Planning,
            response.Status);

        Assert.Equal(
            $"/api/tenants/" +
            $"{tenant.PublicId}/projects/" +
            $"{response.PublicId}",
            createdResult.Location);
    }

    private static Tenant CreatePersistedTenant()
    {
        var tenant =
            new Tenant(
                "Empresa de Teste",
                "REG-PROJECT-CONTROLLER",
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

    [Fact]
    public async Task
    Create_ShouldReturnUnauthorized_WhenUserPublicIdClaimIsMissing()
    {
        var handler =
            new CreateProjectHandler(
                new FakeTenantRepository(),
                new FakeUserRepository(),
                new FakeProjectRepository(),
                new FakeProjectMemberRepository(),
                new FakeUnitOfWork());

        var controller =
            new ProjectsController(
                handler);

        controller.ControllerContext =
            new ControllerContext
            {
                HttpContext =
                    new DefaultHttpContext
                    {
                        User =
                            new ClaimsPrincipal(
                                new ClaimsIdentity(
                                    authenticationType: "Test"))
                    }
            };

        var request =
            new CreateProjectRequest(
                "Projeto de Teste",
                null,
                null);

        var result =
            await controller.Create(
                Guid.NewGuid(),
                request,
                CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(
            result.Result);
    }
}