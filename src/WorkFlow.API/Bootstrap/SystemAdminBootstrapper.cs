using Microsoft.Extensions.Options;
using WorkFlow.Application.Abstractions.Persistence;
using WorkFlow.Application.Abstractions.Security;
using WorkFlow.Application.Common.Security;
using WorkFlow.Domain.Entities;
using WorkFlow.Domain.Enums;

namespace WorkFlow.API.Bootstrap;

public sealed class SystemAdminBootstrapper
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly SystemAdminBootstrapOptions _options;

    public SystemAdminBootstrapper(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IUnitOfWork unitOfWork,
        IOptions<SystemAdminBootstrapOptions> options)
    {
        _userRepository =
            userRepository;

        _passwordHasher =
            passwordHasher;

        _unitOfWork =
            unitOfWork;

        _options =
            options.Value;
    }

    public async Task ExecuteAsync(
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(
                _options.Name))
        {
            throw new InvalidOperationException(
                "O nome do SystemAdmin do bootstrap " +
                "não foi configurado.");
        }

        if (string.IsNullOrWhiteSpace(
                _options.Email))
        {
            throw new InvalidOperationException(
                "O e-mail do SystemAdmin do bootstrap " +
                "não foi configurado.");
        }

        PasswordPolicy.Validate(
            _options.Password,
            nameof(_options.Password));

        var existingSystemAdmin =
            await _userRepository
                .GetSystemAdminByEmailAsync(
                    _options.Email,
                    cancellationToken);

        if (existingSystemAdmin is not null)
        {
            return;
        }

        var passwordHash =
            _passwordHasher.Hash(
                _options.Password);

        var systemAdmin =
            new User(
                null,
                _options.Name,
                _options.Email,
                passwordHash,
                UserRole.SystemAdmin);

        await _userRepository.AddAsync(
            systemAdmin,
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);
    }
}