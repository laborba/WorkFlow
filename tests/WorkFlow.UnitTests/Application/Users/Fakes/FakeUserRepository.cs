using WorkFlow.Application.Abstractions.Persistence;
using WorkFlow.Domain.Entities;
using WorkFlow.Application.Common.Pagination;
using WorkFlow.Domain.Enums;

namespace WorkFlow.UnitTests.Application.Users.Fakes;

internal sealed class FakeUserRepository : IUserRepository
{
    public bool EmailExists { get; set; }

    public long? CheckedTenantId { get; private set; }

    public string? CheckedEmail { get; private set; }

    public User? AddedUser { get; private set; }

    public User? UserToReturn { get; set; }

    public long? CheckedGetByPublicIdTenantId { get; private set; }

    public Guid? CheckedPublicId { get; private set; }

    public IReadOnlyCollection<User> UsersToReturn { get; set; } =
    Array.Empty<User>();

    public long? CheckedGetByEmailTenantId { get; private set; }

    public string? CheckedGetByEmail { get; private set; }

    public int TotalCountToReturn { get; set; }

    public long? CheckedPagedTenantId { get; private set; }

    public int? CheckedPageNumber { get; private set; }

    public int? CheckedPageSize { get; private set; }

    public UserRole? CheckedRole { get; private set; }

    public bool? CheckedIsActive { get; private set; }

    public string? CheckedSearch { get; private set; }

    public long? CheckedGetTrackedByPublicIdTenantId { get; private set; }

    public Guid? CheckedTrackedPublicId { get; private set; }

    
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

    public Task<User?> GetByEmailAsync(
        long tenantId,
        string email,
        CancellationToken cancellationToken = default)
    {
        CheckedGetByEmailTenantId =
            tenantId;

        CheckedGetByEmail =
            email;

        return Task.FromResult(
            UserToReturn);
    }

    public Task<User?> GetByPublicIdAsync(
        long tenantId,
        Guid publicId,
        CancellationToken cancellationToken = default)
    {
        CheckedGetByPublicIdTenantId =
            tenantId;

        CheckedPublicId =
            publicId;

        return Task.FromResult(
            UserToReturn);
    }

    public Task<User?> GetTrackedByPublicIdAsync(
        long tenantId,
        Guid publicId,
        CancellationToken cancellationToken = default)
    {
        CheckedGetTrackedByPublicIdTenantId =
            tenantId;

        CheckedTrackedPublicId =
            publicId;

        return Task.FromResult(
            UserToReturn);
    }

    public Task<PagedData<User>> GetPagedAsync(
        long tenantId,
        int pageNumber,
        int pageSize,
        UserRole? role,
        bool? isActive,
        string? search,
        CancellationToken cancellationToken = default)
    {
        CheckedPagedTenantId =
            tenantId;

        CheckedPageNumber =
            pageNumber;

        CheckedPageSize =
            pageSize;

        CheckedRole =
            role;

        CheckedIsActive =
            isActive;

        CheckedSearch =
            search;

        return Task.FromResult(
            new PagedData<User>(
                UsersToReturn,
                TotalCountToReturn));
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