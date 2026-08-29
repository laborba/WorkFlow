using Microsoft.EntityFrameworkCore;
using WorkFlow.Application.Abstractions.Persistence;
using WorkFlow.Domain.Entities;
using WorkFlow.Domain.Common;
using WorkFlow.Application.Common.Pagination;
using WorkFlow.Domain.Enums;


namespace WorkFlow.Infrastructure.Persistence.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly WorkFlowDbContext _context;

    public UserRepository(
        WorkFlowDbContext context)
    {
        _context = context;
    }

    public Task<bool> ExistsByEmailAsync(
        long tenantId,
        string email,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail =
            EmailNormalizer.Normalize(email);

        return _context.Users.AnyAsync(
            user =>
                user.TenantId == tenantId &&
                user.NormalizedEmail == normalizedEmail,
            cancellationToken);
    }

    public Task<User?> GetByPublicIdAsync(
        long tenantId,
        Guid publicId,
        CancellationToken cancellationToken = default)
    {
        return _context.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(
                user =>
                    user.TenantId == tenantId &&
                    user.PublicId == publicId,
                cancellationToken);
    }

    public Task<User?> GetTrackedByPublicIdAsync(
        long tenantId,
        Guid publicId,
        CancellationToken cancellationToken = default)
    {
        return _context.Users
            .SingleOrDefaultAsync(
                user =>
                    user.TenantId == tenantId &&
                    user.PublicId == publicId,
                cancellationToken);
    }

    public async Task<PagedData<User>> GetPagedAsync(
        long tenantId,
        int pageNumber,
        int pageSize,
        UserRole? role,
        bool? isActive,
        string? search,
        CancellationToken cancellationToken = default)
    {
        var query =
            _context.Users
                .AsNoTracking()
                .Where(user =>
                    user.TenantId == tenantId);

        if (role.HasValue)
        {
            query = query.Where(
                user =>
                    user.Role == role.Value);
        }

        if (isActive.HasValue)
        {
            query = query.Where(
                user =>
                    user.IsActive == isActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchPattern =
                $"%{search.Trim()}%";

            query = query.Where(
                user =>
                    EF.Functions.ILike(
                        user.Name,
                        searchPattern) ||
                    EF.Functions.ILike(
                        user.Email,
                        searchPattern));
        }

        var totalCount =
            await query.CountAsync(
                cancellationToken);

        var items =
            await query
                .OrderBy(user => user.Name)
                .ThenBy(user => user.Id)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

        return new PagedData<User>(
            items,
            totalCount);
    }

    public async Task AddAsync(
        User user,
        CancellationToken cancellationToken = default)
    {
        await _context.Users.AddAsync(
            user,
            cancellationToken);
    }
}