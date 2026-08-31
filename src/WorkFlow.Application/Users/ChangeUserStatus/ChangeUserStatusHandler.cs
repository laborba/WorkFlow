using WorkFlow.Application.Abstractions.Persistence;
using WorkFlow.Application.Common.Results;
using WorkFlow.Application.Tenants;

namespace WorkFlow.Application.Users.ChangeUserStatus;

public sealed class ChangeUserStatusHandler
{
    private readonly ITenantRepository _tenantRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ChangeUserStatusHandler(
        ITenantRepository tenantRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork)
    {
        _tenantRepository = tenantRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ChangeUserStatusResult>> HandleAsync(
        ChangeUserStatusCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.TenantPublicId == Guid.Empty)
        {
            throw new ArgumentException(
                "O PublicId da empresa não pode estar vazio.",
                nameof(command.TenantPublicId));
        }

        if (command.UserPublicId == Guid.Empty)
        {
            throw new ArgumentException(
                "O PublicId do usuário não pode estar vazio.",
                nameof(command.UserPublicId));
        }

        var tenant =
            await _tenantRepository.GetByPublicIdAsync(
                command.TenantPublicId,
                cancellationToken);

        if (tenant is null)
        {
            return Result<ChangeUserStatusResult>.Failure(
                TenantErrors.NotFound);
        }

        if (!tenant.IsActive)
        {
            return Result<ChangeUserStatusResult>.Failure(
                TenantErrors.Inactive);
        }

        var user =
            await _userRepository.GetTrackedByPublicIdAsync(
                tenant.Id,
                command.UserPublicId,
                cancellationToken);

        if (user is null)
        {
            return Result<ChangeUserStatusResult>.Failure(
                UserErrors.NotFound);
        }

        if (command.IsActive)
        {
            user.Activate();
        }
        else
        {
            user.Deactivate();
        }

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Result<ChangeUserStatusResult>.Success(
            new ChangeUserStatusResult(
                user.PublicId,
                tenant.PublicId,
                user.IsActive,
                user.UpdatedAt));
    }
}