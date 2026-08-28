using Microsoft.EntityFrameworkCore;
using WorkFlow.Application.Abstractions.Persistence;
using WorkFlow.Domain.Entities;
using WorkFlow.Domain.Common;

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

    public async Task AddAsync(
        User user,
        CancellationToken cancellationToken = default)
    {
        await _context.Users.AddAsync(
            user,
            cancellationToken);
    }
}