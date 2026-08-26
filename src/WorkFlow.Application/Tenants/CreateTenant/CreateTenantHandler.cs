using WorkFlow.Application.Abstractions.Persistence;
using WorkFlow.Application.Common.Results;
using WorkFlow.Domain.Entities;

namespace WorkFlow.Application.Tenants.CreateTenant;

public sealed class CreateTenantHandler
{
    private readonly ITenantRepository _tenantRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateTenantHandler(
        ITenantRepository tenantRepository,
        IUnitOfWork unitOfWork)
    {
        _tenantRepository = tenantRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<CreateTenantResult>> HandleAsync(
        CreateTenantCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var tenant = new Tenant(
            command.Name,
            command.RegistrationNumber,
            command.Email,
            command.Phone);

        var registrationNumberAlreadyExists =
            await _tenantRepository.ExistsByRegistrationNumberAsync(
                tenant.RegistrationNumber,
                cancellationToken);

        if (registrationNumberAlreadyExists)
        {
            return Result<CreateTenantResult>.Failure(
                TenantErrors.RegistrationNumberAlreadyExists);
        }

        await _tenantRepository.AddAsync(
            tenant,
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Result<CreateTenantResult>.Success(
            new CreateTenantResult(
                tenant.PublicId));
    }
}