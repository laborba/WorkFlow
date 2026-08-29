using WorkFlow.Application.Abstractions.Persistence;
using WorkFlow.Application.Common.Results;
using WorkFlow.Application.Tenants;
using WorkFlow.Domain.Common;

namespace WorkFlow.Application.Users.UpdateUser;

public sealed class UpdateUserHandler
{
    private readonly ITenantRepository _tenantRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateUserHandler(
        ITenantRepository tenantRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork)
    {
        _tenantRepository = tenantRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<UpdateUserResult>> HandleAsync(
        UpdateUserCommand command,
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
            return Result<UpdateUserResult>.Failure(
                TenantErrors.NotFound);
        }

        if (!tenant.IsActive)
        {
            return Result<UpdateUserResult>.Failure(
                TenantErrors.Inactive);
        }

        var user =
            await _userRepository.GetTrackedByPublicIdAsync(
                tenant.Id,
                command.UserPublicId,
                cancellationToken);

        if (user is null)
        {
            return Result<UpdateUserResult>.Failure(
                UserErrors.NotFound);
        }

        var normalizedEmail =
            EmailNormalizer.Normalize(
                command.Email);

        if (user.NormalizedEmail != normalizedEmail)
        {
            var emailAlreadyExists =
                await _userRepository.ExistsByEmailAsync(
                    tenant.Id,
                    command.Email,
                    cancellationToken);

            if (emailAlreadyExists)
            {
                return Result<UpdateUserResult>.Failure(
                    UserErrors.EmailAlreadyExists);
            }
        }

        user.Rename(
            command.Name);

        user.ChangeEmail(
            command.Email);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        var response =
            new UpdateUserResult(
                user.PublicId,
                tenant.PublicId,
                user.Name,
                user.Email,
                user.Role,
                user.IsActive,
                user.CreatedAt,
                user.UpdatedAt);

        return Result<UpdateUserResult>.Success(
            response);
    }
}