using WorkFlow.Domain.Entities;
using WorkFlow.Domain.Enums;

namespace WorkFlow.UnitTests.Domain.Entities;

public class UserTests
{
    [Fact]
    public void Constructor_ShouldCreateActiveUser_WhenDataIsValid()
    {
        // Arrange
        const long tenantId = 1;
        const string name = "Lucas";
        const string email = "lucas@empresa.com";
        const string passwordHash = "fake-password-hash";
        const UserRole role = UserRole.Member;

        // Act
        var user = new User(
            tenantId,
            name,
            email,
            passwordHash,
            role);

        // Assert
        Assert.Equal(tenantId, user.TenantId);
        Assert.Equal(name, user.Name);
        Assert.Equal(email, user.Email);
        Assert.Equal(passwordHash, user.PasswordHash);
        Assert.Equal(role, user.Role);
        Assert.True(user.IsActive);
        Assert.NotEqual(Guid.Empty, user.PublicId);
        Assert.NotEqual(default, user.CreatedAt);
        Assert.Null(user.UpdatedAt);
    }

    [Fact]
    public void Constructor_ShouldAllowSystemAdminWithoutTenant()
    {
        // Act
        var user = new User(
            null,
            "Administrador",
            "admin@workflow.com",
            "fake-password-hash",
            UserRole.SystemAdmin);

        // Assert
        Assert.Null(user.TenantId);
        Assert.Equal(UserRole.SystemAdmin, user.Role);
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenSystemAdminHasTenant()
    {
        Assert.Throws<ArgumentException>(() =>
            new User(
                1,
                "System Administrator",
                "admin@workflow.com",
                "valid-password-hash",
                UserRole.SystemAdmin));
    }

    [Theory]
    [InlineData(UserRole.TenantAdmin)]
    [InlineData(UserRole.ProjectManager)]
    [InlineData(UserRole.Member)]
    public void Constructor_ShouldThrow_WhenNonSystemAdminHasNoTenant(
    UserRole role)
    {
        // Act
        var action = () => new User(
            null,
            "Usuário Teste",
            "usuario@empresa.com",
            "fake-password-hash",
            role);

        // Assert
        Assert.Throws<ArgumentException>(action);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_ShouldThrow_WhenNameIsInvalid(string? invalidName)
    {
        Assert.Throws<ArgumentException>(() =>
            new User(
                1,
                invalidName!,
                "user@email.com",
                "valid-password-hash",
                UserRole.Member));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_ShouldThrow_WhenEmailIsInvalid(string? invalidEmail)
    {
        Assert.Throws<ArgumentException>(() =>
            new User(
                1,
                "Lucas",
                invalidEmail!,
                "valid-password-hash",
                UserRole.Member));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_ShouldThrow_WhenPasswordHashIsInvalid(
        string? invalidPasswordHash)
    {
        Assert.Throws<ArgumentException>(() =>
            new User(
                1,
                "Lucas",
                "user@email.com",
                invalidPasswordHash!,
                UserRole.Member));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(99)]
    public void Constructor_ShouldThrow_WhenRoleIsInvalid(int invalidRoleValue)
    {
        var invalidRole = (UserRole)invalidRoleValue;

        Assert.Throws<ArgumentException>(() =>
            new User(
                1,
                "Lucas",
                "user@email.com",
                "valid-password-hash",
                invalidRole));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_ShouldThrow_WhenTenantIdIsNotPositive(long invalidTenantId)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new User(
                invalidTenantId,
                "Lucas",
                "user@email.com",
                "valid-password-hash",
                UserRole.Member));
    }

    [Fact]
    public void Constructor_ShouldTrimNameAndEmail_WhenTheyHaveOuterSpaces()
    {
        var user = new User(
            1,
            "  Lucas  ",
            "  user@email.com  ",
            "valid-password-hash",
            UserRole.Member);

        Assert.Equal("Lucas", user.Name);
        Assert.Equal("user@email.com", user.Email);
    }

    [Fact]
    public void Constructor_ShouldPreservePasswordHashExactlyAsReceived()
    {
        const string passwordHash = "  valid-password-hash  ";

        var user = new User(
            1,
            "Lucas",
            "user@email.com",
            passwordHash,
            UserRole.Member);

        Assert.Equal(passwordHash, user.PasswordHash);
    }

    [Fact]
    public void Rename_ShouldChangeNameAndSetUpdatedAt_WhenNameIsValid()
    {
        var user = new User(
            1,
            "Lucas",
            "user@email.com",
            "valid-password-hash",
            UserRole.Member);

        user.Rename("  Lucas Aaron  ");

        Assert.Equal("Lucas Aaron", user.Name);
        Assert.NotNull(user.UpdatedAt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Rename_ShouldThrowAndPreserveUser_WhenNameIsInvalid(
    string? invalidName)
    {
        var user = new User(
            1,
            "Lucas",
            "user@email.com",
            "valid-password-hash",
            UserRole.Member);

        Assert.Throws<ArgumentException>(() =>
            user.Rename(invalidName!));

        Assert.Equal("Lucas", user.Name);
        Assert.Null(user.UpdatedAt);
    }

    [Fact]
    public void ChangeEmail_ShouldChangeEmailAndSetUpdatedAt_WhenEmailIsValid()
    {
        var user = new User(
            1,
            "Lucas",
            "old@email.com",
            "valid-password-hash",
            UserRole.Member);

        user.ChangeEmail("  new@email.com  ");

        Assert.Equal("new@email.com", user.Email);
        Assert.NotNull(user.UpdatedAt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ChangeEmail_ShouldThrowAndPreserveUser_WhenEmailIsInvalid(
    string? invalidEmail)
    {
        var user = new User(
            1,
            "Lucas",
            "original@email.com",
            "valid-password-hash",
            UserRole.Member);

        Assert.Throws<ArgumentException>(() =>
            user.ChangeEmail(invalidEmail!));

        Assert.Equal("original@email.com", user.Email);
        Assert.Null(user.UpdatedAt);
    }

    [Fact]
    public void ChangePasswordHash_ShouldChangeHashAndSetUpdatedAt_WhenHashIsValid()
    {
        var user = new User(
            1,
            "Lucas",
            "user@email.com",
            "old-password-hash",
            UserRole.Member);

        const string newPasswordHash = "  new-password-hash  ";

        user.ChangePasswordHash(newPasswordHash);

        Assert.Equal(newPasswordHash, user.PasswordHash);
        Assert.NotNull(user.UpdatedAt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ChangePasswordHash_ShouldThrowAndPreserveUser_WhenHashIsInvalid(
    string? invalidPasswordHash)
    {
        var user = new User(
            1,
            "Lucas",
            "user@email.com",
            "original-password-hash",
            UserRole.Member);

        Assert.Throws<ArgumentException>(() =>
            user.ChangePasswordHash(invalidPasswordHash!));

        Assert.Equal("original-password-hash", user.PasswordHash);
        Assert.Null(user.UpdatedAt);
    }

    [Fact]
    public void Deactivate_ShouldDeactivateUserAndSetUpdatedAt()
    {
        var user = new User(
            1,
            "Lucas",
            "user@email.com",
            "valid-password-hash",
            UserRole.Member);

        user.Deactivate();

        Assert.False(user.IsActive);
        Assert.NotNull(user.UpdatedAt);
    }

    [Fact]
    public void Activate_ShouldActivateUser_WhenUserIsInactive()
    {
        var user = new User(
            1,
            "Lucas",
            "user@email.com",
            "valid-password-hash",
            UserRole.Member);

        user.Deactivate();
        user.Activate();

        Assert.True(user.IsActive);
        Assert.NotNull(user.UpdatedAt);
    }

    [Fact]
    public void ChangeRole_ShouldChangeRoleAndSetUpdatedAt_WhenRoleIsValid()
    {
        var user = new User(
            1,
            "Lucas",
            "user@email.com",
            "valid-password-hash",
            UserRole.Member);

        user.ChangeRole(UserRole.ProjectManager);

        Assert.Equal(UserRole.ProjectManager, user.Role);
        Assert.NotNull(user.UpdatedAt);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(99)]
    public void ChangeRole_ShouldThrowAndPreserveUser_WhenRoleIsInvalid(
    int invalidRoleValue)
    {
        var user = new User(
            1,
            "Lucas",
            "user@email.com",
            "valid-password-hash",
            UserRole.Member);

        var invalidRole = (UserRole)invalidRoleValue;

        Assert.Throws<ArgumentException>(() =>
            user.ChangeRole(invalidRole));

        Assert.Equal(UserRole.Member, user.Role);
        Assert.Null(user.UpdatedAt);
    }

    [Fact]
    public void ChangeRole_ShouldThrow_WhenUserWithoutTenantChangesToTenantRole()
    {
        var user = new User(
            null,
            "System Administrator",
            "admin@workflow.com",
            "valid-password-hash",
            UserRole.SystemAdmin);

        Assert.Throws<ArgumentException>(() =>
            user.ChangeRole(UserRole.Member));

        Assert.Equal(UserRole.SystemAdmin, user.Role);
        Assert.Null(user.UpdatedAt);
    }

    [Fact]
    public void
    ChangeRole_ShouldThrowAndPreserveUser_WhenTenantUserChangesToSystemAdmin()
    {
        var user = new User(
            1,
            "Lucas",
            "user@email.com",
            "valid-password-hash",
            UserRole.Member);

        Assert.Throws<ArgumentException>(() =>
            user.ChangeRole(UserRole.SystemAdmin));

        Assert.Equal(UserRole.Member, user.Role);
        Assert.Equal(1, user.TenantId);
        Assert.Null(user.UpdatedAt);
    }
}