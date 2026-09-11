using WorkFlow.Application.Tenants;
using WorkFlow.Application.Tenants.UpdateTenant;
using WorkFlow.Domain.Entities;
using WorkFlow.UnitTests.Application.Tenants.Fakes;
using WorkFlow.UnitTests.Common.Fakes;

namespace WorkFlow.UnitTests.Application.Tenants.UpdateTenant;

public class UpdateTenantHandlerTests
{
    [Fact]
    public async Task
    HandleAsync_ShouldUpdateTenant_WhenDataIsValid()
    {
        var tenant = new Tenant(
            "Empresa Original",
            "REG-UPDATE-001",
            "original@test.local",
            "(47) 99999-0000");

        var tenantRepository =
            new FakeTenantRepository
            {
                TenantToReturn = tenant
            };

        var unitOfWork =
            new FakeUnitOfWork();

        var handler =
            new UpdateTenantHandler(
                tenantRepository,
                unitOfWork);

        var command =
            new UpdateTenantCommand(
                tenant.PublicId,
                "Empresa Atualizada",
                "atualizada@test.local",
                "(47) 98888-1111");

        var result =
            await handler.HandleAsync(command);

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Null(result.Error);

        var response =
            Assert.IsType<UpdateTenantResult>(
                result.Value);

        Assert.Equal(
            "Empresa Atualizada",
            tenant.Name);

        Assert.Equal(
            "atualizada@test.local",
            tenant.Email);

        Assert.Equal(
            "(47) 98888-1111",
            tenant.Phone);

        Assert.Equal(
            "REG-UPDATE-001",
            tenant.RegistrationNumber);

        Assert.NotNull(
            tenant.UpdatedAt);

        Assert.Equal(
            tenant.PublicId,
            tenantRepository.CheckedPublicIdForUpdate);

        Assert.Equal(
            tenant.PublicId,
            response.PublicId);

        Assert.Equal(
            tenant.Name,
            response.Name);

        Assert.Equal(
            tenant.Email,
            response.Email);

        Assert.Equal(
            tenant.Phone,
            response.Phone);

        Assert.Equal(
            tenant.UpdatedAt,
            response.UpdatedAt);

        Assert.Equal(
            1,
            unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldReturnFailure_WhenTenantDoesNotExist()
    {
        var publicId =
            Guid.NewGuid();

        var tenantRepository =
            new FakeTenantRepository();

        var unitOfWork =
            new FakeUnitOfWork();

        var handler =
            new UpdateTenantHandler(
                tenantRepository,
                unitOfWork);

        var command =
            new UpdateTenantCommand(
                publicId,
                "Empresa Atualizada",
                "atualizada@test.local",
                null);

        var result =
            await handler.HandleAsync(command);

        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Null(result.Value);

        Assert.Equal(
            TenantErrors.NotFound,
            result.Error);

        Assert.Equal(
            publicId,
            tenantRepository.CheckedPublicIdForUpdate);

        Assert.Equal(
            0,
            unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldThrow_WhenPublicIdIsEmpty()
    {
        var tenantRepository =
            new FakeTenantRepository();

        var unitOfWork =
            new FakeUnitOfWork();

        var handler =
            new UpdateTenantHandler(
                tenantRepository,
                unitOfWork);

        var command =
            new UpdateTenantCommand(
                Guid.Empty,
                "Empresa Atualizada",
                "atualizada@test.local",
                null);

        var exception =
            await Assert.ThrowsAsync<ArgumentException>(
                () => handler.HandleAsync(command));

        Assert.Equal(
            "PublicId",
            exception.ParamName);

        Assert.Null(
            tenantRepository.CheckedPublicIdForUpdate);

        Assert.Equal(
            0,
            unitOfWork.SaveChangesCallCount);
    }
}