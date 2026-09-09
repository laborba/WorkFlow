using WorkFlow.Application.Abstractions.Persistence;
using WorkFlow.Application.Abstractions.Persistence.Models;
using WorkFlow.Application.Common.Pagination;
using WorkFlow.Domain.Entities;

namespace WorkFlow.UnitTests.Application.Projects.Fakes;

internal sealed class FakeProjectMemberRepository :
    IProjectMemberRepository
{
    public ProjectMember? AddedProjectMember { get; private set; }

    public bool IsActiveMemberResult { get; set; }

    public Dictionary<(long ProjectId, long UserId), bool>
        IsActiveMemberResults
    { get; } = new();

    public List<(long ProjectId, long UserId)>
        IsActiveMemberCalls
    { get; } = new();

    public long? QueriedProjectId { get; private set; }

    public long? QueriedUserId { get; private set; }

    public PagedData<ProjectMemberListItemData>
        PagedDataToReturn
    { get; set; } =
            new(
                Array.Empty<ProjectMemberListItemData>(),
                0);

    public long? LastPagedProjectId { get; private set; }

    public int? LastPageNumber { get; private set; }

    public int? LastPageSize { get; private set; }

    public string? LastSearch { get; private set; }

    public Task<bool> IsActiveMemberAsync(
        long projectId,
        long userId,
        CancellationToken cancellationToken = default)
    {
        QueriedProjectId =
            projectId;

        QueriedUserId =
            userId;

        IsActiveMemberCalls.Add(
            (projectId, userId));

        if (IsActiveMemberResults.TryGetValue(
                (projectId, userId),
                out var result))
        {
            return Task.FromResult(
                result);
        }

        return Task.FromResult(
            IsActiveMemberResult);
    }

    public Task<PagedData<ProjectMemberListItemData>> GetPagedAsync(
        long projectId,
        int pageNumber,
        int pageSize,
        string? search,
        CancellationToken cancellationToken = default)
    {
        LastPagedProjectId =
            projectId;

        LastPageNumber =
            pageNumber;

        LastPageSize =
            pageSize;

        LastSearch =
            search;

        return Task.FromResult(
            PagedDataToReturn);
    }

    public Task AddAsync(
        ProjectMember projectMember,
        CancellationToken cancellationToken = default)
    {
        AddedProjectMember =
            projectMember;

        return Task.CompletedTask;
    }
}