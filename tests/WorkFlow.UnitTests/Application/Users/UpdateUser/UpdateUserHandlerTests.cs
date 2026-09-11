using WorkFlow.Application.Tenants;
using WorkFlow.Application.Users;
using WorkFlow.Application.Users.UpdateUser;
using WorkFlow.Domain.Entities;
using WorkFlow.Domain.Enums;
using WorkFlow.UnitTests.Application.Tenants.Fakes;
using WorkFlow.UnitTests.Application.Users.Fakes;
using WorkFlow.UnitTests.Common;
using WorkFlow.UnitTests.Common.Fakes;

namespace WorkFlow.UnitTests.Application.Users.UpdateUser;

public class UpdateUserHandlerTests
{
    [Fact]
    public async Task
    HandleAsync_ShouldUpdateUser_WhenDataIsValid()
    {
        var tenant =
            CreatePersistedTenant();

        var user =
            new User(
                tenant.Id,
                "Usuário Original",
                "original@test.local",
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
            new UpdateUserHandler(
                tenantRepository,
                userRepository,
                unitOfWork);

        var command =
            new UpdateUserCommand(
                tenant.PublicId,
                user.PublicId,
                "Usuário Atualizado",
                "atualizado@test.local");

        var result =
            await handler.HandleAsync(command);

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Null(result.Error);

        var response =
            Assert.IsType<UpdateUserResult>(
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
            tenant.Id,
            userRepository.CheckedTenantId);

        Assert.Equal(
            command.Email,
            userRepository.CheckedEmail);

        Assert.Equal(
            command.Name,
            user.Name);

        Assert.Equal(
            command.Email,
            user.Email);

        Assert.Equal(
            command.Name,
            response.Name);

        Assert.Equal(
            command.Email,
            response.Email);

        Assert.Equal(
            user.PublicId,
            response.PublicId);

        Assert.Equal(
            tenant.PublicId,
            response.TenantPublicId);

        Assert.Equal(
            user.Role,
            response.Role);

        Assert.Equal(
            user.IsActive,
            response.IsActive);

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
    HandleAsync_ShouldNotCheckEmailDuplication_WhenNormalizedEmailIsUnchanged()
    {
        var tenant =
            CreatePersistedTenant();

        var user =
            new User(
                tenant.Id,
                "Usuário Original",
                "Usuario@Test.Local",
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
                UserToReturn = user,
                EmailExists = true
            };

        var unitOfWork =
            new FakeUnitOfWork();

        var handler =
            new UpdateUserHandler(
                tenantRepository,
                userRepository,
                unitOfWork);

        var command =
            new UpdateUserCommand(
                tenant.PublicId,
                user.PublicId,
                "Usuário Atualizado",
                "usuario@test.local");

        var result =
            await handler.HandleAsync(command);

        Assert.True(
            result.IsSuccess);

        Assert.Null(
            userRepository.CheckedTenantId);

        Assert.Null(
            userRepository.CheckedEmail);

        Assert.Equal(
            command.Name,
            user.Name);

        Assert.Equal(
            command.Email,
            user.Email);

        Assert.Equal(
            1,
            unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldReturnFailure_WhenEmailAlreadyExists()
    {
        var tenant =
            CreatePersistedTenant();

        var user =
            new User(
                tenant.Id,
                "Usuário Original",
                "original@test.local",
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
                UserToReturn = user,
                EmailExists = true
            };

        var unitOfWork =
            new FakeUnitOfWork();

        var handler =
            new UpdateUserHandler(
                tenantRepository,
                userRepository,
                unitOfWork);

        var command =
            new UpdateUserCommand(
                tenant.PublicId,
                user.PublicId,
                "Usuário Atualizado",
                "outro@test.local");

        var result =
            await handler.HandleAsync(command);

        Assert.True(
            result.IsFailure);

        Assert.False(
            result.IsSuccess);

        Assert.Null(
            result.Value);

        Assert.Equal(
            UserErrors.EmailAlreadyExists,
            result.Error);

        Assert.Equal(
            tenant.Id,
            userRepository.CheckedTenantId);

        Assert.Equal(
            command.Email,
            userRepository.CheckedEmail);

        Assert.Equal(
            "Usuário Original",
            user.Name);

        Assert.Equal(
            "original@test.local",
            user.Email);

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
            new UpdateUserHandler(
                tenantRepository,
                userRepository,
                unitOfWork);

        var userPublicId =
            Guid.NewGuid();

        var command =
            new UpdateUserCommand(
                tenant.PublicId,
                userPublicId,
                "Usuário Atualizado",
                "atualizado@test.local");

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

        Assert.Null(
            userRepository.CheckedTenantId);

        Assert.Null(
            userRepository.CheckedEmail);

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
            new UpdateUserHandler(
                tenantRepository,
                userRepository,
                unitOfWork);

        var command =
            new UpdateUserCommand(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "Usuário Atualizado",
                "atualizado@test.local");

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
            new UpdateUserHandler(
                tenantRepository,
                userRepository,
                unitOfWork);

        var command =
            new UpdateUserCommand(
                tenant.PublicId,
                Guid.NewGuid(),
                "Usuário Atualizado",
                "atualizado@test.local");

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
            new UpdateUserHandler(
                tenantRepository,
                userRepository,
                unitOfWork);

        var command =
            new UpdateUserCommand(
                Guid.Empty,
                Guid.NewGuid(),
                "Usuário Atualizado",
                "atualizado@test.local");

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
            new UpdateUserHandler(
                tenantRepository,
                userRepository,
                unitOfWork);

        var command =
            new UpdateUserCommand(
                Guid.NewGuid(),
                Guid.Empty,
                "Usuário Atualizado",
                "atualizado@test.local");

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

    [Fact]
    public async Task
    HandleAsync_ShouldThrow_WhenNameIsEmpty()
    {
        var tenant =
            CreatePersistedTenant();

        var user =
            new User(
                tenant.Id,
                "Usuário Original",
                "original@test.local",
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
            new UpdateUserHandler(
                tenantRepository,
                userRepository,
                unitOfWork);

        var command =
            new UpdateUserCommand(
                tenant.PublicId,
                user.PublicId,
                "   ",
                user.Email);

        var exception =
            await Assert.ThrowsAsync<ArgumentException>(
                () => handler.HandleAsync(command));

        Assert.Equal(
            "name",
            exception.ParamName);

        Assert.Equal(
            0,
            unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldThrow_WhenEmailIsEmpty()
    {
        var tenant =
            CreatePersistedTenant();

        var user =
            new User(
                tenant.Id,
                "Usuário Original",
                "original@test.local",
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
            new UpdateUserHandler(
                tenantRepository,
                userRepository,
                unitOfWork);

        var command =
            new UpdateUserCommand(
                tenant.PublicId,
                user.PublicId,
                "Usuário Atualizado",
                "   ");

        var exception =
            await Assert.ThrowsAsync<ArgumentException>(
                () => handler.HandleAsync(command));

        Assert.Equal(
            "email",
            exception.ParamName);

        Assert.Equal(
            "Usuário Original",
            user.Name);

        Assert.Equal(
            "original@test.local",
            user.Email);

        Assert.Equal(
            0,
            unitOfWork.SaveChangesCallCount);
    }

    private static Tenant CreatePersistedTenant()
    {
        var tenant =
            new Tenant(
                "Empresa de Teste",
                "REG-UPDATE-USER",
                "empresa@test.local");

        EntityTestHelper.SetId(
            tenant,
            42);

        return tenant;
    }
}