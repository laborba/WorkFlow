using WorkFlow.Domain.Entities;
using WorkFlow.Application.Common.Pagination;
using WorkFlow.Domain.Enums;

namespace WorkFlow.Application.Abstractions.Persistence;

public interface IUserRepository
{
    Task<bool> ExistsByEmailAsync(
        long tenantId,
        string email,
        CancellationToken cancellationToken = default);

    Task<User?> GetByEmailAsync(
        long tenantId,
        string email,
        CancellationToken cancellationToken = default);

    Task<User?> GetSystemAdminByEmailAsync(
        string email,
        CancellationToken cancellationToken = default);

    Task<User?> GetByPublicIdAsync(
        long tenantId,
        Guid publicId,
        CancellationToken cancellationToken = default);

    Task<User?> GetByIdAsync(
    long tenantId,
    long userId,
    CancellationToken cancellationToken = default);

    Task<User?> GetTrackedByPublicIdAsync(
        long tenantId,
        Guid publicId,
        CancellationToken cancellationToken = default);

    Task<PagedData<User>> GetPagedAsync(
        long tenantId,
        int pageNumber,
        int pageSize,
        UserRole? role,
        bool? isActive,
        string? search,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        User user,
        CancellationToken cancellationToken = default);
}