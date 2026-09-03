using Microsoft.Extensions.Options;
using WorkFlow.API.Bootstrap;
using WorkFlow.UnitTests.Application.Tenants.Fakes;
using WorkFlow.UnitTests.Application.Users.Fakes;

namespace WorkFlow.UnitTests.API.Bootstrap;

public sealed class SystemAdminBootstrapperTests
{
    [Fact]
    public async Task
    ExecuteAsync_ShouldDoNothing_WhenBootstrapIsDisabled()
    {
        var userRepository =
            new FakeUserRepository();

        var passwordHasher =
            new FakePasswordHasher();

        var unitOfWork =
            new FakeUnitOfWork();

        var options =
            Options.Create(
                new SystemAdminBootstrapOptions
                {
                    Enabled = false
                });

        var bootstrapper =
            new SystemAdminBootstrapper(
                userRepository,
                passwordHasher,
                unitOfWork,
                options);

        await bootstrapper.ExecuteAsync();

        Assert.Null(
            userRepository.CheckedGetSystemAdminByEmail);

        Assert.Null(
            userRepository.AddedUser);

        Assert.Null(
            passwordHasher.PasswordReceivedForHash);

        Assert.Equal(
            0,
            unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task
    ExecuteAsync_ShouldDoNothing_WhenSystemAdminAlreadyExists()
    {
        var existingSystemAdmin =
            new WorkFlow.Domain.Entities.User(
                null,
                "Administrador do Sistema",
                "admin@workflow.test",
                "stored-password-hash",
                WorkFlow.Domain.Enums.UserRole.SystemAdmin);

        var userRepository =
            new FakeUserRepository
            {
                UserToReturn = existingSystemAdmin
            };

        var passwordHasher =
            new FakePasswordHasher();

        var unitOfWork =
            new FakeUnitOfWork();

        var options =
            Options.Create(
                new SystemAdminBootstrapOptions
                {
                    Enabled = true,
                    Name = "Administrador do Sistema",
                    Email = "admin@workflow.test",
                    Password = "senha-segura-123"
                });

        var bootstrapper =
            new SystemAdminBootstrapper(
                userRepository,
                passwordHasher,
                unitOfWork,
                options);

        await bootstrapper.ExecuteAsync();

        Assert.Equal(
            options.Value.Email,
            userRepository.CheckedGetSystemAdminByEmail);

        Assert.Null(
            userRepository.AddedUser);

        Assert.Null(
            passwordHasher.PasswordReceivedForHash);

        Assert.Equal(
            0,
            unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task
    ExecuteAsync_ShouldCreateSystemAdmin_WhenSystemAdminDoesNotExist()
    {
        var userRepository =
            new FakeUserRepository
            {
                UserToReturn = null
            };

        var passwordHasher =
            new FakePasswordHasher
            {
                HashToReturn =
                    "generated-password-hash"
            };

        var unitOfWork =
            new FakeUnitOfWork();

        var options =
            Options.Create(
                new SystemAdminBootstrapOptions
                {
                    Enabled = true,
                    Name = "Administrador do Sistema",
                    Email = "admin@workflow.test",
                    Password = "senha-segura-123"
                });

        var bootstrapper =
            new SystemAdminBootstrapper(
                userRepository,
                passwordHasher,
                unitOfWork,
                options);

        await bootstrapper.ExecuteAsync();

        Assert.Equal(
            options.Value.Email,
            userRepository.CheckedGetSystemAdminByEmail);

        Assert.Equal(
            options.Value.Password,
            passwordHasher.PasswordReceivedForHash);

        Assert.NotNull(
            userRepository.AddedUser);

        Assert.Null(
            userRepository.AddedUser!.TenantId);

        Assert.Equal(
            options.Value.Name,
            userRepository.AddedUser.Name);

        Assert.Equal(
            options.Value.Email,
            userRepository.AddedUser.Email);

        Assert.Equal(
            "generated-password-hash",
            userRepository.AddedUser.PasswordHash);

        Assert.Equal(
            WorkFlow.Domain.Enums.UserRole.SystemAdmin,
            userRepository.AddedUser.Role);

        Assert.True(
            userRepository.AddedUser.IsActive);

        Assert.Equal(
            1,
            unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task
    ExecuteAsync_ShouldThrow_WhenNameIsEmpty()
    {
        var userRepository =
            new FakeUserRepository();

        var passwordHasher =
            new FakePasswordHasher();

        var unitOfWork =
            new FakeUnitOfWork();

        var options =
            Options.Create(
                new SystemAdminBootstrapOptions
                {
                    Enabled = true,
                    Name = "   ",
                    Email = "admin@workflow.test",
                    Password = "senha-segura-123"
                });

        var bootstrapper =
            new SystemAdminBootstrapper(
                userRepository,
                passwordHasher,
                unitOfWork,
                options);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => bootstrapper.ExecuteAsync());

        Assert.Null(
            userRepository.CheckedGetSystemAdminByEmail);

        Assert.Null(
            userRepository.AddedUser);

        Assert.Null(
            passwordHasher.PasswordReceivedForHash);

        Assert.Equal(
            0,
            unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task
    ExecuteAsync_ShouldThrow_WhenEmailIsEmpty()
    {
        var userRepository =
            new FakeUserRepository();

        var passwordHasher =
            new FakePasswordHasher();

        var unitOfWork =
            new FakeUnitOfWork();

        var options =
            Options.Create(
                new SystemAdminBootstrapOptions
                {
                    Enabled = true,
                    Name = "Administrador do Sistema",
                    Email = "   ",
                    Password = "senha-segura-123"
                });

        var bootstrapper =
            new SystemAdminBootstrapper(
                userRepository,
                passwordHasher,
                unitOfWork,
                options);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => bootstrapper.ExecuteAsync());

        Assert.Null(
            userRepository.CheckedGetSystemAdminByEmail);

        Assert.Null(
            userRepository.AddedUser);

        Assert.Null(
            passwordHasher.PasswordReceivedForHash);

        Assert.Equal(
            0,
            unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task
    ExecuteAsync_ShouldThrow_WhenPasswordIsInvalid()
    {
        var userRepository =
            new FakeUserRepository();

        var passwordHasher =
            new FakePasswordHasher();

        var unitOfWork =
            new FakeUnitOfWork();

        var options =
            Options.Create(
                new SystemAdminBootstrapOptions
                {
                    Enabled = true,
                    Name = "Administrador do Sistema",
                    Email = "admin@workflow.test",
                    Password = "curta"
                });

        var bootstrapper =
            new SystemAdminBootstrapper(
                userRepository,
                passwordHasher,
                unitOfWork,
                options);

        var exception =
            await Assert.ThrowsAsync<ArgumentException>(
                () => bootstrapper.ExecuteAsync());

        Assert.Equal(
            "Password",
            exception.ParamName);

        Assert.Null(
            userRepository.CheckedGetSystemAdminByEmail);

        Assert.Null(
            userRepository.AddedUser);

        Assert.Null(
            passwordHasher.PasswordReceivedForHash);

        Assert.Equal(
            0,
            unitOfWork.SaveChangesCallCount);
    }
}