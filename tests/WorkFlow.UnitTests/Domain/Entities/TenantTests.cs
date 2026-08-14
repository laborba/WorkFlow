using WorkFlow.Domain.Entities;

namespace WorkFlow.UnitTests.Domain.Entities;

public class TenantTests
{
    [Fact]
    public void Constructor_ShouldCreateActiveTenant_WhenDataIsValid()
    {
        // Arrange
        const string name = "Empresa Teste";
        const string registrationNumber = "123456789";
        const string email = "contato@empresa.com";

        // Act
        var tenant = new Tenant(name, registrationNumber, email);

        // Assert
        Assert.Equal(name, tenant.Name);
        Assert.Equal(registrationNumber, tenant.RegistrationNumber);
        Assert.Equal(email, tenant.Email);
        Assert.Null(tenant.Phone);
        Assert.True(tenant.IsActive);
        Assert.NotEqual(Guid.Empty, tenant.PublicId);
        Assert.NotEqual(default, tenant.CreatedAt);
        Assert.Null(tenant.UpdatedAt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_ShouldThrow_WhenNameIsInvalid(string? name)
    {
        // Act
        var action = () => new Tenant(
            name!,
            "123456789",
            "contato@empresa.com");

        // Assert
        Assert.Throws<ArgumentException>(action);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_ShouldThrow_WhenRegistrationNumberIsInvalid(
    string? registrationNumber)
    {
        // Act
        var action = () => new Tenant(
            "Empresa Teste",
            registrationNumber!,
            "contato@empresa.com");

        // Assert
        Assert.Throws<ArgumentException>(action);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_ShouldThrow_WhenEmailIsInvalid(string? email)
    {
        // Act
        var action = () => new Tenant(
            "Empresa Teste",
            "123456789",
            email!);

        // Assert
        Assert.Throws<ArgumentException>(action);
    }

    [Fact]
    public void Constructor_ShouldTrimTextValues()
    {
        // Act
        var tenant = new Tenant(
            "  Empresa Teste  ",
            "  123456789  ",
            "  contato@empresa.com  ",
            "  47999999999  ");

        // Assert
        Assert.Equal("Empresa Teste", tenant.Name);
        Assert.Equal("123456789", tenant.RegistrationNumber);
        Assert.Equal("contato@empresa.com", tenant.Email);
        Assert.Equal("47999999999", tenant.Phone);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_ShouldSetPhoneToNull_WhenPhoneIsEmpty(
    string? phone)
    {
        // Act
        var tenant = new Tenant(
            "Empresa Teste",
            "123456789",
            "contato@empresa.com",
            phone);

        // Assert
        Assert.Null(tenant.Phone);
    }

    [Fact]
    public void Rename_ShouldChangeNameAndSetUpdatedAt_WhenNameIsValid()
    {
        // Arrange
        var tenant = new Tenant(
            "Empresa Antiga",
            "123456789",
            "contato@empresa.com");

        // Act
        tenant.Rename("Empresa Nova");

        // Assert
        Assert.Equal("Empresa Nova", tenant.Name);
        Assert.NotNull(tenant.UpdatedAt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Rename_ShouldThrow_WhenNameIsInvalid(string? name)
    {
        // Arrange
        var tenant = new Tenant(
            "Empresa Teste",
            "123456789",
            "contato@empresa.com");

        // Act
        var action = () => tenant.Rename(name!);

        // Assert
        Assert.Throws<ArgumentException>(action);
    }

    [Fact]
    public void Deactivate_ShouldSetTenantAsInactive()
    {
        // Arrange
        var tenant = new Tenant(
            "Empresa Teste",
            "123456789",
            "contato@empresa.com");

        // Act
        tenant.Deactivate();

        // Assert
        Assert.False(tenant.IsActive);
        Assert.NotNull(tenant.UpdatedAt);
    }

    [Fact]
    public void Activate_ShouldSetTenantAsActive_WhenTenantIsInactive()
    {
        // Arrange
        var tenant = new Tenant(
            "Empresa Teste",
            "123456789",
            "contato@empresa.com");

        tenant.Deactivate();

        // Act
        tenant.Activate();

        // Assert
        Assert.True(tenant.IsActive);
        Assert.NotNull(tenant.UpdatedAt);
    }

    [Fact]
    public void UpdateContact_ShouldChangeEmailAndPhone()
    {
        // Arrange
        var tenant = new Tenant(
            "Empresa Teste",
            "123456789",
            "antigo@empresa.com");

        // Act
        tenant.UpdateContact(
            "novo@empresa.com",
            "47999999999");

        // Assert
        Assert.Equal("novo@empresa.com", tenant.Email);
        Assert.Equal("47999999999", tenant.Phone);
        Assert.NotNull(tenant.UpdatedAt);
    }
}