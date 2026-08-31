using WorkFlow.Application.Tenants;
using WorkFlow.Application.Users;
using WorkFlow.Application.Users.ChangeUserRole;
using WorkFlow.Domain.Entities;
using WorkFlow.Domain.Enums;
using WorkFlow.UnitTests.Application.Tenants.Fakes;
using WorkFlow.UnitTests.Application.Users.Fakes;
using WorkFlow.UnitTests.Common;

namespace WorkFlow.UnitTests.Application.Users.ChangeUserRole;

public class ChangeUserRoleHandlerTests
{
    [Fact]
    public async Task
    HandleAsync_ShouldChangeUserRole_WhenDataIsValid()
    {
        var tenant =
            CreatePersistedTenant();

        var user =
            new User(
                tenant.Id,
                "Usuário de Teste",
                "usuario@test.local",
                "password-hash",
                UserRole.Member);

        var tenantRepository =
            new FakeTenantRepository
            {
                TenantToReturn = tenant
            };

        var userRepository =
            new FakeUserRepository
            {
                UserToReturn = user
            };

        var unitOfWork =
            new FakeUnitOfWork();

        var handler =
            new ChangeUserRoleHandler(
                tenantRepository,
                userRepository,
                unitOfWork);

        var command =
            new ChangeUserRoleCommand(
                tenant.PublicId,
                user.PublicId,
                UserRole.ProjectManager);

        var result =
            await handler.HandleAsync(command);

        Assert.True(
            result.IsSuccess);

        Assert.False(
            result.IsFailure);

        Assert.Null(
            result.Error);

        var response =
            Assert.IsType<ChangeUserRoleResult>(
                result.Value);

        Assert.Equal(
            tenant.PublicId,
            tenantRepository.CheckedPublicId);

        Assert.Equal(
            tenant.Id,
            userRepository.CheckedGetTrackedByPublicIdTenantId);

        Assert.Equal(
            user.PublicId,
            userRepository.CheckedTrackedPublicId);

        Assert.Equal(
            UserRole.ProjectManager,
            user.Role);

        Assert.Equal(
            UserRole.ProjectManager,
            response.Role);

        Assert.Equal(
            user.PublicId,
            response.PublicId);

        Assert.Equal(
            tenant.PublicId,
            response.TenantPublicId);

        Assert.NotNull(
            user.UpdatedAt);

        Assert.Equal(
            user.UpdatedAt,
            response.UpdatedAt);

        Assert.Equal(
            1,
            unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldReturnFailure_WhenRoleIsSystemAdmin()
    {
        var tenant =
            CreatePersistedTenant();

        var user =
            new User(
                tenant.Id,
                "Usuário de Teste",
                "usuario@test.local",
                "password-hash",
                UserRole.Member);

        var tenantRepository =
            new FakeTenantRepository();

        var userRepository =
            new FakeUserRepository
            {
                UserToReturn = user
            };

        var unitOfWork =
            new FakeUnitOfWork();

        var handler =
            new ChangeUserRoleHandler(
                tenantRepository,
                userRepository,
                unitOfWork);

        var command =
            new ChangeUserRoleCommand(
                tenant.PublicId,
                user.PublicId,
                UserRole.SystemAdmin);

        var result =
            await handler.HandleAsync(command);

        Assert.True(
            result.IsFailure);

        Assert.False(
            result.IsSuccess);

        Assert.Null(
            result.Value);

        Assert.Equal(
            UserErrors.SystemAdminCannotBelongToTenant,
            result.Error);

        Assert.Null(
            tenantRepository.CheckedPublicId);

        Assert.Null(
            userRepository.CheckedGetTrackedByPublicIdTenantId);

        Assert.Equal(
            UserRole.Member,
            user.Role);

        Assert.Equal(
            0,
            unitOfWork.SaveChangesCallCount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(99)]
    public async Task
    HandleAsync_ShouldThrow_WhenRoleIsInvalid(
    int roleValue)
    {
        var tenantRepository =
            new FakeTenantRepository();

        var userRepository =
            new FakeUserRepository();

        var unitOfWork =
            new FakeUnitOfWork();

        var handler =
            new ChangeUserRoleHandler(
                tenantRepository,
                userRepository,
                unitOfWork);

        var command =
            new ChangeUserRoleCommand(
                Guid.NewGuid(),
                Guid.NewGuid(),
                (UserRole)roleValue);

        var exception =
            await Assert.ThrowsAsync<ArgumentException>(
                () => handler.HandleAsync(command));

        Assert.Equal(
            "Role",
            exception.ParamName);

        Assert.Null(
            tenantRepository.CheckedPublicId);

        Assert.Null(
            userRepository.CheckedGetTrackedByPublicIdTenantId);

        Assert.Equal(
            0,
            unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldReturnFailure_WhenTenantDoesNotExist()
    {
        var tenantRepository =
            new FakeTenantRepository();

        var userRepository =
            new FakeUserRepository();

        var unitOfWork =
            new FakeUnitOfWork();

        var handler =
            new ChangeUserRoleHandler(
                tenantRepository,
                userRepository,
                unitOfWork);

        var command =
            new ChangeUserRoleCommand(
                Guid.NewGuid(),
                Guid.NewGuid(),
                UserRole.Member);

        var result =
            await handler.HandleAsync(command);

        Assert.True(
            result.IsFailure);

        Assert.False(
            result.IsSuccess);

        Assert.Null(
            result.Value);

        Assert.Equal(
            TenantErrors.NotFound,
            result.Error);

        Assert.Null(
            userRepository.CheckedGetTrackedByPublicIdTenantId);

        Assert.Null(
            userRepository.CheckedTrackedPublicId);

        Assert.Equal(
            0,
            unitOfWork.SaveChangesCallCount);
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

        var unitOfWork =
            new FakeUnitOfWork();

        var handler =
            new ChangeUserRoleHandler(
                tenantRepository,
                userRepository,
                unitOfWork);

        var command =
            new ChangeUserRoleCommand(
                tenant.PublicId,
                Guid.NewGuid(),
                UserRole.ProjectManager);

        var result =
            await handler.HandleAsync(command);

        Assert.True(
            result.IsFailure);

        Assert.False(
            result.IsSuccess);

        Assert.Null(
            result.Value);

        Assert.Equal(
            TenantErrors.Inactive,
            result.Error);

        Assert.Null(
            userRepository.CheckedGetTrackedByPublicIdTenantId);

        Assert.Null(
            userRepository.CheckedTrackedPublicId);

        Assert.Equal(
            0,
            unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldReturnFailure_WhenUserDoesNotExist()
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

        var unitOfWork =
            new FakeUnitOfWork();

        var handler =
            new ChangeUserRoleHandler(
                tenantRepository,
                userRepository,
                unitOfWork);

        var userPublicId =
            Guid.NewGuid();

        var command =
            new ChangeUserRoleCommand(
                tenant.PublicId,
                userPublicId,
                UserRole.ProjectManager);

        var result =
            await handler.HandleAsync(command);

        Assert.True(
            result.IsFailure);

        Assert.False(
            result.IsSuccess);

        Assert.Null(
            result.Value);

        Assert.Equal(
            UserErrors.NotFound,
            result.Error);

        Assert.Equal(
            tenant.Id,
            userRepository.CheckedGetTrackedByPublicIdTenantId);

        Assert.Equal(
            userPublicId,
            userRepository.CheckedTrackedPublicId);

        Assert.Equal(
            0,
            unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldThrow_WhenTenantPublicIdIsEmpty()
    {
        var tenantRepository =
            new FakeTenantRepository();

        var userRepository =
            new FakeUserRepository();

        var unitOfWork =
            new FakeUnitOfWork();

        var handler =
            new ChangeUserRoleHandler(
                tenantRepository,
                userRepository,
                unitOfWork);

        var command =
            new ChangeUserRoleCommand(
                Guid.Empty,
                Guid.NewGuid(),
                UserRole.Member);

        var exception =
            await Assert.ThrowsAsync<ArgumentException>(
                () => handler.HandleAsync(command));

        Assert.Equal(
            "TenantPublicId",
            exception.ParamName);

        Assert.Null(
            tenantRepository.CheckedPublicId);

        Assert.Null(
            userRepository.CheckedGetTrackedByPublicIdTenantId);

        Assert.Equal(
            0,
            unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldThrow_WhenUserPublicIdIsEmpty()
    {
        var tenantRepository =
            new FakeTenantRepository();

        var userRepository =
            new FakeUserRepository();

        var unitOfWork =
            new FakeUnitOfWork();

        var handler =
            new ChangeUserRoleHandler(
                tenantRepository,
                userRepository,
                unitOfWork);

        var command =
            new ChangeUserRoleCommand(
                Guid.NewGuid(),
                Guid.Empty,
                UserRole.Member);

        var exception =
            await Assert.ThrowsAsync<ArgumentException>(
                () => handler.HandleAsync(command));

        Assert.Equal(
            "UserPublicId",
            exception.ParamName);

        Assert.Null(
            tenantRepository.CheckedPublicId);

        Assert.Null(
            userRepository.CheckedGetTrackedByPublicIdTenantId);

        Assert.Equal(
            0,
            unitOfWork.SaveChangesCallCount);
    }

    private static Tenant CreatePersistedTenant()
    {
        var tenant =
            new Tenant(
                "Empresa de Teste",
                "REG-CHANGE-USER-ROLE",
                "empresa@test.local");

        EntityTestHelper.SetId(
            tenant,
            42);

        return tenant;
    }
}