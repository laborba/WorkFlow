using WorkFlow.Application.Abstractions.Persistence;
using WorkFlow.Domain.Entities;

namespace WorkFlow.UnitTests.Application.Users.Fakes;

internal sealed class FakeUserRepository : IUserRepository
{
    public bool EmailExists { get; set; }

    public long? CheckedTenantId { get; private set; }

    public string? CheckedEmail { get; private set; }

    public User? AddedUser { get; private set; }

    public Task<bool> ExistsByEmailAsync(
        long tenantId,
        string email,
        CancellationToken cancellationToken = default)
    {
        CheckedTenantId =
            tenantId;

        CheckedEmail =
            email;

        return Task.FromResult(
            EmailExists);
    }

    public Task AddAsync(
        User user,
        CancellationToken cancellationToken = default)
    {
        AddedUser =
            user;

        return Task.CompletedTask;
    }
}