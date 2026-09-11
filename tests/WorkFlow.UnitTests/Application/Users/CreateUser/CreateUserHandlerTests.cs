using WorkFlow.Application.Tenants;
using WorkFlow.Application.Users;
using WorkFlow.Application.Users.CreateUser;
using WorkFlow.Domain.Entities;
using WorkFlow.Domain.Enums;
using WorkFlow.UnitTests.Application.Tenants.Fakes;
using WorkFlow.UnitTests.Application.Users.Fakes;
using WorkFlow.UnitTests.Common;
using WorkFlow.UnitTests.Common.Fakes;

namespace WorkFlow.UnitTests.Application.Users.CreateUser;

public class CreateUserHandlerTests
{
    private const string ValidPassword =
        "senha-original-segura";

    [Fact]
    public async Task
    HandleAsync_ShouldCreateUser_WhenDataIsValid()
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

        var passwordHasher =
            new FakePasswordHasher
            {
                HashToReturn = "secure-password-hash"
            };

        var unitOfWork =
            new FakeUnitOfWork();

        var handler =
            new CreateUserHandler(
                tenantRepository,
                userRepository,
                passwordHasher,
                unitOfWork);

        var command =
            new CreateUserCommand(
                tenant.PublicId,
                "Novo Usuário",
                "usuario@test.local",
                ValidPassword,
                UserRole.Member);

        var result =
            await handler.HandleAsync(command);

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Null(result.Error);

        var response =
            Assert.IsType<CreateUserResult>(
                result.Value);

        Assert.Equal(
            tenant.PublicId,
            tenantRepository.CheckedPublicId);

        Assert.Equal(
            tenant.Id,
            userRepository.CheckedTenantId);

        Assert.Equal(
            command.Email,
            userRepository.CheckedEmail);

        Assert.Equal(
            command.Password,
            passwordHasher.PasswordReceivedForHash);

        var addedUser =
            Assert.IsType<User>(
                userRepository.AddedUser);

        Assert.Equal(
            tenant.Id,
            addedUser.TenantId);

        Assert.Equal(
            command.Name,
            addedUser.Name);

        Assert.Equal(
            command.Email,
            addedUser.Email);

        Assert.Equal(
            "secure-password-hash",
            addedUser.PasswordHash);

        Assert.NotEqual(
            command.Password,
            addedUser.PasswordHash);

        Assert.Equal(
            UserRole.Member,
            addedUser.Role);

        Assert.True(
            addedUser.IsActive);

        Assert.Equal(
            addedUser.PublicId,
            response.PublicId);

        Assert.Equal(
            tenant.PublicId,
            response.TenantPublicId);

        Assert.Equal(
            addedUser.Name,
            response.Name);

        Assert.Equal(
            addedUser.Email,
            response.Email);

        Assert.Equal(
            addedUser.Role,
            response.Role);

        Assert.Equal(
            addedUser.IsActive,
            response.IsActive);

        Assert.Equal(
            addedUser.CreatedAt,
            response.CreatedAt);

        Assert.Equal(
            1,
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

        var passwordHasher =
            new FakePasswordHasher();

        var unitOfWork =
            new FakeUnitOfWork();

        var handler =
            new CreateUserHandler(
                tenantRepository,
                userRepository,
                passwordHasher,
                unitOfWork);

        var command =
            new CreateUserCommand(
                Guid.NewGuid(),
                "Novo Usuário",
                "usuario@test.local",
                ValidPassword,
                UserRole.Member);

        var result =
            await handler.HandleAsync(command);

        Assert.True(result.IsFailure);
        Assert.False(result.IsSuccess);
        Assert.Null(result.Value);

        Assert.Equal(
            TenantErrors.NotFound,
            result.Error);

        Assert.Null(
            userRepository.CheckedTenantId);

        Assert.Null(
            passwordHasher.PasswordReceivedForHash);

        Assert.Null(
            userRepository.AddedUser);

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

        var passwordHasher =
            new FakePasswordHasher();

        var unitOfWork =
            new FakeUnitOfWork();

        var handler =
            new CreateUserHandler(
                tenantRepository,
                userRepository,
                passwordHasher,
                unitOfWork);

        var command =
            new CreateUserCommand(
                tenant.PublicId,
                "Novo Usuário",
                "usuario@test.local",
                ValidPassword,
                UserRole.Member);

        var result =
            await handler.HandleAsync(command);

        Assert.True(result.IsFailure);

        Assert.Equal(
            TenantErrors.Inactive,
            result.Error);

        Assert.Null(
            userRepository.CheckedTenantId);

        Assert.Null(
            passwordHasher.PasswordReceivedForHash);

        Assert.Null(
            userRepository.AddedUser);

        Assert.Equal(
            0,
            unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldReturnFailure_WhenEmailAlreadyExists()
    {
        var tenant =
            CreatePersistedTenant();

        var tenantRepository =
            new FakeTenantRepository
            {
                TenantToReturn = tenant
            };

        var userRepository =
            new FakeUserRepository
            {
                EmailExists = true
            };

        var passwordHasher =
            new FakePasswordHasher();

        var unitOfWork =
            new FakeUnitOfWork();

        var handler =
            new CreateUserHandler(
                tenantRepository,
                userRepository,
                passwordHasher,
                unitOfWork);

        var command =
            new CreateUserCommand(
                tenant.PublicId,
                "Novo Usuário",
                "usuario@test.local",
                ValidPassword,
                UserRole.Member);

        var result =
            await handler.HandleAsync(command);

        Assert.True(result.IsFailure);

        Assert.Equal(
            UserErrors.EmailAlreadyExists,
            result.Error);

        Assert.Equal(
            tenant.Id,
            userRepository.CheckedTenantId);

        Assert.Equal(
            command.Email,
            userRepository.CheckedEmail);

        Assert.Null(
            passwordHasher.PasswordReceivedForHash);

        Assert.Null(
            userRepository.AddedUser);

        Assert.Equal(
            0,
            unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldReturnFailure_WhenRoleIsSystemAdmin()
    {
        var tenantRepository =
            new FakeTenantRepository();

        var userRepository =
            new FakeUserRepository();

        var passwordHasher =
            new FakePasswordHasher();

        var unitOfWork =
            new FakeUnitOfWork();

        var handler =
            new CreateUserHandler(
                tenantRepository,
                userRepository,
                passwordHasher,
                unitOfWork);

        var command =
            new CreateUserCommand(
                Guid.NewGuid(),
                "Administrador do Sistema",
                "system@test.local",
                ValidPassword,
                UserRole.SystemAdmin);

        var result =
            await handler.HandleAsync(command);

        Assert.True(result.IsFailure);

        Assert.Equal(
            UserErrors.SystemAdminCannotBelongToTenant,
            result.Error);

        Assert.Null(
            tenantRepository.CheckedPublicId);

        Assert.Null(
            userRepository.AddedUser);

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

        var passwordHasher =
            new FakePasswordHasher();

        var unitOfWork =
            new FakeUnitOfWork();

        var handler =
            new CreateUserHandler(
                tenantRepository,
                userRepository,
                passwordHasher,
                unitOfWork);

        var command =
            new CreateUserCommand(
                Guid.Empty,
                "Novo Usuário",
                "usuario@test.local",
                ValidPassword,
                UserRole.Member);

        var exception =
            await Assert.ThrowsAsync<ArgumentException>(
                () => handler.HandleAsync(command));

        Assert.Equal(
            "TenantPublicId",
            exception.ParamName);

        Assert.Null(
            tenantRepository.CheckedPublicId);

        Assert.Equal(
            0,
            unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldThrow_WhenPasswordIsEmpty()
    {
        var tenantRepository =
            new FakeTenantRepository();

        var userRepository =
            new FakeUserRepository();

        var passwordHasher =
            new FakePasswordHasher();

        var unitOfWork =
            new FakeUnitOfWork();

        var handler =
            new CreateUserHandler(
                tenantRepository,
                userRepository,
                passwordHasher,
                unitOfWork);

        var command =
            new CreateUserCommand(
                Guid.NewGuid(),
                "Novo Usuário",
                "usuario@test.local",
                "   ",
                UserRole.Member);

        var exception =
            await Assert.ThrowsAsync<ArgumentException>(
                () => handler.HandleAsync(command));

        Assert.Equal(
            "Password",
            exception.ParamName);

        Assert.Null(
            tenantRepository.CheckedPublicId);

        Assert.Null(
            passwordHasher.PasswordReceivedForHash);

        Assert.Equal(
            0,
            unitOfWork.SaveChangesCallCount);
    }

    [Theory]
    [InlineData(14)]
    [InlineData(129)]
    public async Task
    HandleAsync_ShouldThrow_WhenPasswordLengthIsInvalid(
        int passwordLength)
    {
        var tenantRepository =
            new FakeTenantRepository();

        var userRepository =
            new FakeUserRepository();

        var passwordHasher =
            new FakePasswordHasher();

        var unitOfWork =
            new FakeUnitOfWork();

        var handler =
            new CreateUserHandler(
                tenantRepository,
                userRepository,
                passwordHasher,
                unitOfWork);

        var command =
            new CreateUserCommand(
                Guid.NewGuid(),
                "Novo Usuário",
                "usuario@test.local",
                new string('a', passwordLength),
                UserRole.Member);

        var exception =
            await Assert.ThrowsAsync<ArgumentException>(
                () => handler.HandleAsync(command));

        Assert.Equal(
            "Password",
            exception.ParamName);

        Assert.Contains(
            "entre 15 e 128 caracteres",
            exception.Message);

        Assert.Null(
            tenantRepository.CheckedPublicId);

        Assert.Null(
            passwordHasher.PasswordReceivedForHash);

        Assert.Equal(
            0,
            unitOfWork.SaveChangesCallCount);
    }

    private static Tenant CreatePersistedTenant()
    {
        var tenant = new Tenant(
            "Empresa de Teste",
            "REG-CREATE-USER",
            "empresa@test.local");

        EntityTestHelper.SetId(
            tenant,
            42);

        return tenant;
    }
}