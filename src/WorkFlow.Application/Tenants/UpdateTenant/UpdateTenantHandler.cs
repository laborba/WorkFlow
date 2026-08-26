using WorkFlow.Application.Abstractions.Persistence;
using WorkFlow.Application.Common.Results;

namespace WorkFlow.Application.Tenants.UpdateTenant;

public sealed class UpdateTenantHandler
{
    private readonly ITenantRepository _tenantRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateTenantHandler(
        ITenantRepository tenantRepository,
        IUnitOfWork unitOfWork)
    {
        _tenantRepository = tenantRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<UpdateTenantResult>> HandleAsync(
        UpdateTenantCommand command,
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
            return Result<UpdateTenantResult>.Failure(
                TenantErrors.NotFound);
        }

        tenant.Rename(command.Name);

        tenant.UpdateContact(
            command.Email,
            command.Phone);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Result<UpdateTenantResult>.Success(
            new UpdateTenantResult(
                tenant.PublicId,
                tenant.Name,
                tenant.Email,
                tenant.Phone,
                tenant.UpdatedAt));
    }
}