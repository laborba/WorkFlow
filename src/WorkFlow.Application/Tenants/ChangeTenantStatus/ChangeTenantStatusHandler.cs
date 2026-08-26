using WorkFlow.Application.Abstractions.Persistence;
using WorkFlow.Application.Common.Results;

namespace WorkFlow.Application.Tenants.ChangeTenantStatus;

public sealed class ChangeTenantStatusHandler
{
    private readonly ITenantRepository _tenantRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ChangeTenantStatusHandler(
        ITenantRepository tenantRepository,
        IUnitOfWork unitOfWork)
    {
        _tenantRepository = tenantRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ChangeTenantStatusResult>> HandleAsync(
        ChangeTenantStatusCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.PublicId == Guid.Empty)
        {
            throw new ArgumentException(
                "O identificador público da empresa não pode estar vazio.",
                nameof(command.PublicId));
        }

        var tenant =
            await _tenantRepository.GetForUpdateByPublicIdAsync(
                command.PublicId,
                cancellationToken);

        if (tenant is null)
        {
            return Result<ChangeTenantStatusResult>.Failure(
                TenantErrors.NotFound);
        }

        if (command.IsActive)
        {
            tenant.Activate();
        }
        else
        {
            tenant.Deactivate();
        }

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Result<ChangeTenantStatusResult>.Success(
            new ChangeTenantStatusResult(
                tenant.PublicId,
                tenant.IsActive,
                tenant.UpdatedAt));
    }
}