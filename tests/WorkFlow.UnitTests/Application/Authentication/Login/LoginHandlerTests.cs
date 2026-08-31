using WorkFlow.Application.Authentication;
using WorkFlow.Application.Authentication.Login;
using WorkFlow.Application.Tenants;
using WorkFlow.Domain.Entities;
using WorkFlow.Domain.Enums;
using WorkFlow.UnitTests.Application.Authentication.Fakes;
using WorkFlow.UnitTests.Application.Tenants.Fakes;
using WorkFlow.UnitTests.Application.Users.Fakes;
using WorkFlow.UnitTests.Common;

namespace WorkFlow.UnitTests.Application.Authentication.Login;

public sealed class LoginHandlerTests
{
    [Fact]
    public async Task
    HandleAsync_ShouldReturnLoginResult_WhenCredentialsAreValid()
    {
        var tenant =
            new Tenant(
                "Empresa de Teste",
                "registration-login-test",
                "tenant@login.test");

        EntityTestHelper.SetId(
            tenant,
            42);

        var user =
            new User(
                tenant.Id,
                "Usuário de Teste",
                "usuario@login.test",
                "stored-password-hash",
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

        var passwordHasher =
            new FakePasswordHasher
            {
                VerificationResult = true
            };

        var expiresAt =
            DateTime.UtcNow.AddHours(1);

        var accessTokenGenerator =
            new FakeAccessTokenGenerator
            {
                AccessTokenToReturn =
                    "generated-access-token",

                ExpiresAtToReturn =
                    expiresAt
            };

        var handler =
            new LoginHandler(
                tenantRepository,
                userRepository,
                passwordHasher,
                accessTokenGenerator);

        var command =
            new LoginCommand(
                tenant.PublicId,
                "usuario@login.test",
                "valid-password");

        var result =
            await handler.HandleAsync(command);

        Assert.True(
            result.IsSuccess);

        Assert.NotNull(
            result.Value);

        Assert.Equal(
            tenant.PublicId,
            tenantRepository.CheckedPublicId);

        Assert.Equal(
            tenant.Id,
            userRepository.CheckedGetByEmailTenantId);

        Assert.Equal(
            command.Email,
            userRepository.CheckedGetByEmail);

        Assert.Equal(
            command.Password,
            passwordHasher.PasswordReceivedForVerification);

        Assert.Equal(
            user.PasswordHash,
            passwordHasher.HashReceivedForVerification);

        Assert.Same(
            user,
            accessTokenGenerator.UserReceived);

        Assert.Equal(
            tenant.PublicId,
            accessTokenGenerator.TenantPublicIdReceived);

        Assert.Equal(
            "generated-access-token",
            result.Value!.AccessToken);

        Assert.Equal(
            expiresAt,
            result.Value.ExpiresAt);

        Assert.Equal(
            user.PublicId,
            result.Value.UserPublicId);

        Assert.Equal(
            tenant.PublicId,
            result.Value.TenantPublicId);

        Assert.Equal(
            user.Name,
            result.Value.Name);

        Assert.Equal(
            user.Email,
            result.Value.Email);

        Assert.Equal(
            user.Role,
            result.Value.Role);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldReturnFailure_WhenTenantDoesNotExist()
    {
        var tenantPublicId =
            Guid.NewGuid();

        var tenantRepository =
            new FakeTenantRepository
            {
                TenantToReturn = null
            };

        var userRepository =
            new FakeUserRepository();

        var passwordHasher =
            new FakePasswordHasher();

        var accessTokenGenerator =
            new FakeAccessTokenGenerator();

        var handler =
            new LoginHandler(
                tenantRepository,
                userRepository,
                passwordHasher,
                accessTokenGenerator);

        var command =
            new LoginCommand(
                tenantPublicId,
                "usuario@login.test",
                "valid-password");

        var result =
            await handler.HandleAsync(command);

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            TenantErrors.NotFound,
            result.Error);

        Assert.Equal(
            tenantPublicId,
            tenantRepository.CheckedPublicId);

        Assert.Null(
            userRepository.CheckedGetByEmailTenantId);

        Assert.Null(
            passwordHasher.PasswordReceivedForVerification);

        Assert.Null(
            accessTokenGenerator.UserReceived);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldReturnFailure_WhenTenantIsInactive()
    {
        var tenant =
            new Tenant(
                "Empresa de Teste",
                "registration-inactive-login-test",
                "tenant-inactive@login.test");

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

        var accessTokenGenerator =
            new FakeAccessTokenGenerator();

        var handler =
            new LoginHandler(
                tenantRepository,
                userRepository,
                passwordHasher,
                accessTokenGenerator);

        var command =
            new LoginCommand(
                tenant.PublicId,
                "usuario@login.test",
                "valid-password");

        var result =
            await handler.HandleAsync(command);

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            TenantErrors.Inactive,
            result.Error);

        Assert.Equal(
            tenant.PublicId,
            tenantRepository.CheckedPublicId);

        Assert.Null(
            userRepository.CheckedGetByEmailTenantId);

        Assert.Null(
            passwordHasher.PasswordReceivedForVerification);

        Assert.Null(
            accessTokenGenerator.UserReceived);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldReturnInvalidCredentials_WhenUserDoesNotExist()
    {
        var tenant =
            new Tenant(
                "Empresa de Teste",
                "registration-user-not-found-login",
                "tenant-user-not-found@login.test");

        EntityTestHelper.SetId(
            tenant,
            42);

        var tenantRepository =
            new FakeTenantRepository
            {
                TenantToReturn = tenant
            };

        var userRepository =
            new FakeUserRepository
            {
                UserToReturn = null
            };

        var passwordHasher =
            new FakePasswordHasher();

        var accessTokenGenerator =
            new FakeAccessTokenGenerator();

        var handler =
            new LoginHandler(
                tenantRepository,
                userRepository,
                passwordHasher,
                accessTokenGenerator);

        var command =
            new LoginCommand(
                tenant.PublicId,
                "inexistente@login.test",
                "some-password");

        var result =
            await handler.HandleAsync(command);

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            AuthenticationErrors.InvalidCredentials,
            result.Error);

        Assert.Equal(
            tenant.Id,
            userRepository.CheckedGetByEmailTenantId);

        Assert.Equal(
            command.Email,
            userRepository.CheckedGetByEmail);

        Assert.Null(
            passwordHasher.PasswordReceivedForVerification);

        Assert.Null(
            accessTokenGenerator.UserReceived);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldReturnInvalidCredentials_WhenPasswordIsInvalid()
    {
        var tenant =
            new Tenant(
                "Empresa de Teste",
                "registration-invalid-password-login",
                "tenant-invalid-password@login.test");

        EntityTestHelper.SetId(
            tenant,
            42);

        var user =
            new User(
                tenant.Id,
                "Usuário de Teste",
                "usuario@login.test",
                "stored-password-hash",
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

        var passwordHasher =
            new FakePasswordHasher
            {
                VerificationResult = false
            };

        var accessTokenGenerator =
            new FakeAccessTokenGenerator();

        var handler =
            new LoginHandler(
                tenantRepository,
                userRepository,
                passwordHasher,
                accessTokenGenerator);

        var command =
            new LoginCommand(
                tenant.PublicId,
                "usuario@login.test",
                "wrong-password");

        var result =
            await handler.HandleAsync(command);

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            AuthenticationErrors.InvalidCredentials,
            result.Error);

        Assert.Equal(
            command.Password,
            passwordHasher.PasswordReceivedForVerification);

        Assert.Equal(
            user.PasswordHash,
            passwordHasher.HashReceivedForVerification);

        Assert.Null(
            accessTokenGenerator.UserReceived);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldReturnUserInactive_WhenUserIsInactiveAndPasswordIsValid()
    {
        var tenant =
            new Tenant(
                "Empresa de Teste",
                "registration-inactive-user-login",
                "tenant-inactive-user@login.test");

        EntityTestHelper.SetId(
            tenant,
            42);

        var user =
            new User(
                tenant.Id,
                "Usuário Inativo",
                "usuario-inativo@login.test",
                "stored-password-hash",
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

        var passwordHasher =
            new FakePasswordHasher
            {
                VerificationResult = true
            };

        var accessTokenGenerator =
            new FakeAccessTokenGenerator();

        var handler =
            new LoginHandler(
                tenantRepository,
                userRepository,
                passwordHasher,
                accessTokenGenerator);

        var command =
            new LoginCommand(
                tenant.PublicId,
                "usuario-inativo@login.test",
                "valid-password");

        var result =
            await handler.HandleAsync(command);

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            AuthenticationErrors.UserInactive,
            result.Error);

        Assert.Equal(
            command.Password,
            passwordHasher.PasswordReceivedForVerification);

        Assert.Equal(
            user.PasswordHash,
            passwordHasher.HashReceivedForVerification);

        Assert.Null(
            accessTokenGenerator.UserReceived);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldReturnInvalidCredentials_WhenUserIsInactiveAndPasswordIsInvalid()
    {
        var tenant =
            new Tenant(
                "Empresa de Teste",
                "registration-inactive-user-wrong-password",
                "tenant-inactive-user-wrong-password@login.test");

        EntityTestHelper.SetId(
            tenant,
            42);

        var user =
            new User(
                tenant.Id,
                "Usuário Inativo",
                "usuario-inativo@login.test",
                "stored-password-hash",
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

        var passwordHasher =
            new FakePasswordHasher
            {
                VerificationResult = false
            };

        var accessTokenGenerator =
            new FakeAccessTokenGenerator();

        var handler =
            new LoginHandler(
                tenantRepository,
                userRepository,
                passwordHasher,
                accessTokenGenerator);

        var command =
            new LoginCommand(
                tenant.PublicId,
                "usuario-inativo@login.test",
                "wrong-password");

        var result =
            await handler.HandleAsync(command);

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            AuthenticationErrors.InvalidCredentials,
            result.Error);

        Assert.Equal(
            command.Password,
            passwordHasher.PasswordReceivedForVerification);

        Assert.Equal(
            user.PasswordHash,
            passwordHasher.HashReceivedForVerification);

        Assert.Null(
            accessTokenGenerator.UserReceived);
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

        var accessTokenGenerator =
            new FakeAccessTokenGenerator();

        var handler =
            new LoginHandler(
                tenantRepository,
                userRepository,
                passwordHasher,
                accessTokenGenerator);

        var command =
            new LoginCommand(
                Guid.Empty,
                "usuario@login.test",
                "valid-password");

        var exception =
            await Assert.ThrowsAsync<ArgumentException>(
                () => handler.HandleAsync(command));

        Assert.Equal(
            "TenantPublicId",
            exception.ParamName);

        Assert.Null(
            tenantRepository.CheckedPublicId);

        Assert.Null(
            userRepository.CheckedGetByEmailTenantId);

        Assert.Null(
            passwordHasher.PasswordReceivedForVerification);

        Assert.Null(
            accessTokenGenerator.UserReceived);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldThrow_WhenEmailIsEmpty()
    {
        var tenantRepository =
            new FakeTenantRepository();

        var userRepository =
            new FakeUserRepository();

        var passwordHasher =
            new FakePasswordHasher();

        var accessTokenGenerator =
            new FakeAccessTokenGenerator();

        var handler =
            new LoginHandler(
                tenantRepository,
                userRepository,
                passwordHasher,
                accessTokenGenerator);

        var command =
            new LoginCommand(
                Guid.NewGuid(),
                "   ",
                "valid-password");

        var exception =
            await Assert.ThrowsAsync<ArgumentException>(
                () => handler.HandleAsync(command));

        Assert.Equal(
            "Email",
            exception.ParamName);

        Assert.Null(
            tenantRepository.CheckedPublicId);

        Assert.Null(
            userRepository.CheckedGetByEmailTenantId);

        Assert.Null(
            passwordHasher.PasswordReceivedForVerification);

        Assert.Null(
            accessTokenGenerator.UserReceived);
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

        var accessTokenGenerator =
            new FakeAccessTokenGenerator();

        var handler =
            new LoginHandler(
                tenantRepository,
                userRepository,
                passwordHasher,
                accessTokenGenerator);

        var command =
            new LoginCommand(
                Guid.NewGuid(),
                "usuario@login.test",
                "   ");

        var exception =
            await Assert.ThrowsAsync<ArgumentException>(
                () => handler.HandleAsync(command));

        Assert.Equal(
            "Password",
            exception.ParamName);

        Assert.Null(
            tenantRepository.CheckedPublicId);

        Assert.Null(
            userRepository.CheckedGetByEmailTenantId);

        Assert.Null(
            passwordHasher.PasswordReceivedForVerification);

        Assert.Null(
            accessTokenGenerator.UserReceived);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldThrow_WhenCommandIsNull()
    {
        var tenantRepository =
            new FakeTenantRepository();

        var userRepository =
            new FakeUserRepository();

        var passwordHasher =
            new FakePasswordHasher();

        var accessTokenGenerator =
            new FakeAccessTokenGenerator();

        var handler =
            new LoginHandler(
                tenantRepository,
                userRepository,
                passwordHasher,
                accessTokenGenerator);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => handler.HandleAsync(null!));

        Assert.Null(
            tenantRepository.CheckedPublicId);

        Assert.Null(
            userRepository.CheckedGetByEmailTenantId);

        Assert.Null(
            passwordHasher.PasswordReceivedForVerification);

        Assert.Null(
            accessTokenGenerator.UserReceived);
    }
}