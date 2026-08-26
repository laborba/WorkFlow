using WorkFlow.Application.Tenants.CreateTenant;
using WorkFlow.Domain.Entities;
using WorkFlow.UnitTests.Application.Tenants.Fakes;
using WorkFlow.Application.Tenants;

namespace WorkFlow.UnitTests.Application.Tenants.CreateTenant;

public class CreateTenantHandlerTests
{
    [Fact]
    public async Task
    HandleAsync_ShouldCreateTenant_WhenRegistrationNumberDoesNotExist()
    {
        var tenantRepository =
            new FakeTenantRepository();

        var unitOfWork =
            new FakeUnitOfWork();

        var handler = new CreateTenantHandler(
            tenantRepository,
            unitOfWork);

        var command = new CreateTenantCommand(
            " Empresa Teste ",
            " REG-123 ",
            " empresa@test.local ",
            " (47) 99999-9999 ");

        var result =
            await handler.HandleAsync(command);

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Null(result.Error);

        var response =
            Assert.IsType<CreateTenantResult>(
                result.Value);

        Assert.NotEqual(
            Guid.Empty,
            response.PublicId);

        var addedTenant =
            Assert.IsType<Tenant>(
                tenantRepository.AddedTenant);

        Assert.Equal(
            "Empresa Teste",
            addedTenant.Name);

        Assert.Equal(
            "REG-123",
            addedTenant.RegistrationNumber);

        Assert.Equal(
            "empresa@test.local",
            addedTenant.Email);

        Assert.Equal(
            "(47) 99999-9999",
            addedTenant.Phone);

        Assert.Equal(
            addedTenant.PublicId,
            response.PublicId);

        Assert.Equal(
            "REG-123",
            tenantRepository.CheckedRegistrationNumber);

        Assert.Equal(
            1,
            unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldReturnFailure_WhenRegistrationNumberAlreadyExists()
    {
        var tenantRepository =
            new FakeTenantRepository
            {
                RegistrationNumberExists = true
            };

        var unitOfWork =
            new FakeUnitOfWork();

        var handler = new CreateTenantHandler(
            tenantRepository,
            unitOfWork);

        var command = new CreateTenantCommand(
            "Empresa Duplicada",
            " REG-123 ",
            "duplicada@test.local");

        var result =
            await handler.HandleAsync(command);

        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Null(result.Value);

        Assert.Equal(
            TenantErrors.RegistrationNumberAlreadyExists,
            result.Error);

        Assert.Equal(
            "REG-123",
            tenantRepository.CheckedRegistrationNumber);

        Assert.Null(
            tenantRepository.AddedTenant);

        Assert.Equal(
            0,
            unitOfWork.SaveChangesCallCount);
    }
}