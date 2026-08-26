using WorkFlow.Application.Tenants.ChangeTenantStatus;
using WorkFlow.Domain.Entities;
using WorkFlow.UnitTests.Application.Tenants.Fakes;
using WorkFlow.Application.Tenants;

namespace WorkFlow.UnitTests.Application.Tenants.ChangeTenantStatus;

public class ChangeTenantStatusHandlerTests
{
    [Fact]
    public async Task
    HandleAsync_ShouldDeactivateTenant_WhenTenantIsActive()
    {
        var tenant = new Tenant(
            "Empresa Ativa",
            "REG-STATUS-001",
            "ativa@test.local");

        var tenantRepository =
            new FakeTenantRepository
            {
                TenantToReturn = tenant
            };

        var unitOfWork =
            new FakeUnitOfWork();

        var handler =
            new ChangeTenantStatusHandler(
                tenantRepository,
                unitOfWork);

        var command =
            new ChangeTenantStatusCommand(
                tenant.PublicId,
                false);

        var result =
            await handler.HandleAsync(command);

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Null(result.Error);

        var response =
            Assert.IsType<ChangeTenantStatusResult>(
                result.Value);

        Assert.False(tenant.IsActive);
        Assert.NotNull(tenant.UpdatedAt);

        Assert.Equal(
            tenant.PublicId,
            tenantRepository.CheckedPublicIdForUpdate);

        Assert.Equal(
            tenant.PublicId,
            response.PublicId);

        Assert.False(
            response.IsActive);

        Assert.Equal(
            tenant.UpdatedAt,
            response.UpdatedAt);

        Assert.Equal(
            1,
            unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldActivateTenant_WhenTenantIsInactive()
    {
        var tenant = new Tenant(
            "Empresa Inativa",
            "REG-STATUS-002",
            "inativa@test.local");

        tenant.Deactivate();

        var deactivatedAt =
            tenant.UpdatedAt;

        var tenantRepository =
            new FakeTenantRepository
            {
                TenantToReturn = tenant
            };

        var unitOfWork =
            new FakeUnitOfWork();

        var handler =
            new ChangeTenantStatusHandler(
                tenantRepository,
                unitOfWork);

        var command =
            new ChangeTenantStatusCommand(
                tenant.PublicId,
                true);

        var result =
            await handler.HandleAsync(command);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Error);

        var response =
            Assert.IsType<ChangeTenantStatusResult>(
                result.Value);

        Assert.True(tenant.IsActive);
        Assert.NotNull(tenant.UpdatedAt);

        Assert.True(
            tenant.UpdatedAt >= deactivatedAt);

        Assert.Equal(
            tenant.PublicId,
            response.PublicId);

        Assert.True(
            response.IsActive);

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
            new ChangeTenantStatusHandler(
                tenantRepository,
                unitOfWork);

        var command =
            new ChangeTenantStatusCommand(
                publicId,
                false);

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
            new ChangeTenantStatusHandler(
                tenantRepository,
                unitOfWork);

        var command =
            new ChangeTenantStatusCommand(
                Guid.Empty,
                false);

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