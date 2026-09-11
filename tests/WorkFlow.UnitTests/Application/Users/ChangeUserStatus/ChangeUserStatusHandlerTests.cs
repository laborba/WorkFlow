using WorkFlow.Application.Users;
using WorkFlow.Application.Users.ChangeUserStatus;
using WorkFlow.Domain.Entities;
using WorkFlow.Domain.Enums;
using WorkFlow.UnitTests.Application.Tenants.Fakes;
using WorkFlow.UnitTests.Application.Users.Fakes;
using WorkFlow.UnitTests.Common;
using WorkFlow.Application.Tenants;
using WorkFlow.UnitTests.Common.Fakes;

namespace WorkFlow.UnitTests.Application.Users.ChangeUserStatus;

public class ChangeUserStatusHandlerTests
{
    [Fact]
    public async Task
    HandleAsync_ShouldDeactivateUser_WhenIsActiveIsFalse()
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
            new ChangeUserStatusHandler(
                tenantRepository,
                userRepository,
                unitOfWork);

        var command =
            new ChangeUserStatusCommand(
                tenant.PublicId,
                user.PublicId,
                false);

        var result =
            await handler.HandleAsync(command);

        Assert.True(
            result.IsSuccess);

        Assert.False(
            result.IsFailure);

        Assert.Null(
            result.Error);

        var response =
            Assert.IsType<ChangeUserStatusResult>(
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

        Assert.False(
            user.IsActive);

        Assert.False(
            response.IsActive);

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

    private static Tenant CreatePersistedTenant()
    {
        var tenant =
            new Tenant(
                "Empresa de Teste",
                "REG-CHANGE-USER-STATUS",
                "empresa@test.local");

        EntityTestHelper.SetId(
            tenant,
            42);

        return tenant;
    }

    [Fact]
    public async Task
    HandleAsync_ShouldActivateUser_WhenIsActiveIsTrue()
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

        user.Deactivate();

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
            new ChangeUserStatusHandler(
                tenantRepository,
                userRepository,
                unitOfWork);

        var command =
            new ChangeUserStatusCommand(
                tenant.PublicId,
                user.PublicId,
                true);

        var result =
            await handler.HandleAsync(command);

        Assert.True(
            result.IsSuccess);

        Assert.False(
            result.IsFailure);

        Assert.Null(
            result.Error);

        var response =
            Assert.IsType<ChangeUserStatusResult>(
                result.Value);

        Assert.True(
            user.IsActive);

        Assert.True(
            response.IsActive);

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
            new ChangeUserStatusHandler(
                tenantRepository,
                userRepository,
                unitOfWork);

        var userPublicId =
            Guid.NewGuid();

        var command =
            new ChangeUserStatusCommand(
                tenant.PublicId,
                userPublicId,
                false);

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
HandleAsync_ShouldReturnFailure_WhenTenantDoesNotExist()
    {
        var tenantRepository =
            new FakeTenantRepository();

        var userRepository =
            new FakeUserRepository();

        var unitOfWork =
            new FakeUnitOfWork();

        var handler =
            new ChangeUserStatusHandler(
                tenantRepository,
                userRepository,
                unitOfWork);

        var command =
            new ChangeUserStatusCommand(
                Guid.NewGuid(),
                Guid.NewGuid(),
                false);

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
            new ChangeUserStatusHandler(
                tenantRepository,
                userRepository,
                unitOfWork);

        var command =
            new ChangeUserStatusCommand(
                tenant.PublicId,
                Guid.NewGuid(),
                false);

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
HandleAsync_ShouldThrow_WhenTenantPublicIdIsEmpty()
    {
        var tenantRepository =
            new FakeTenantRepository();

        var userRepository =
            new FakeUserRepository();

        var unitOfWork =
            new FakeUnitOfWork();

        var handler =
            new ChangeUserStatusHandler(
                tenantRepository,
                userRepository,
                unitOfWork);

        var command =
            new ChangeUserStatusCommand(
                Guid.Empty,
                Guid.NewGuid(),
                false);

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
            new ChangeUserStatusHandler(
                tenantRepository,
                userRepository,
                unitOfWork);

        var command =
            new ChangeUserStatusCommand(
                Guid.NewGuid(),
                Guid.Empty,
                false);

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
}