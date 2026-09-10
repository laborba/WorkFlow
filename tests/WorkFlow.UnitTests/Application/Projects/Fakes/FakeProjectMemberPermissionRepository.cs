using WorkFlow.Application.Abstractions.Persistence;
using WorkFlow.Application.Abstractions.Persistence.Models;
using WorkFlow.Domain.Entities;
using WorkFlow.Domain.Enums;

namespace WorkFlow.UnitTests.Application.Projects.Fakes;

internal sealed class FakeProjectMemberPermissionRepository :
    IProjectMemberPermissionRepository
{
    public bool IsActivePermissionResult { get; set; }

    public Dictionary<
        (long ProjectMemberId, ProjectPermission Permission),
        bool>
        IsActivePermissionResults
    { get; } = new();

    public List<
        (long ProjectMemberId, ProjectPermission Permission)>
        IsActivePermissionCalls
    { get; } = new();

    public Dictionary<
        (long ProjectMemberId, ProjectPermission Permission),
        ProjectMemberPermission?>
        ActivePermissionsForUpdateToReturn
    { get; } = new();

    public List<
        (long ProjectMemberId, ProjectPermission Permission)>
        GetActiveForUpdateCalls
    { get; } = new();

    public ProjectMemberPermission?
        ActivePermissionForUpdateToReturn
    { get; set; }

    public IReadOnlyCollection<ProjectMemberPermissionListItemData>
        ActivePermissionsToReturn
    { get; set; } =
            Array.Empty<ProjectMemberPermissionListItemData>();

    public long?
        LastGetActivePermissionsProjectMemberId
    { get; private set; }

    public ProjectMemberPermission?
        AddedProjectMemberPermission
    { get; private set; }

    public List<ProjectMemberPermission>
        AddedProjectMemberPermissions
    { get; } = new();

    public Task<bool> IsActivePermissionAsync(
        long projectMemberId,
        ProjectPermission permission,
        CancellationToken cancellationToken = default)
    {
        IsActivePermissionCalls.Add(
            (projectMemberId, permission));

        if (IsActivePermissionResults.TryGetValue(
                (projectMemberId, permission),
                out var result))
        {
            return Task.FromResult(
                result);
        }

        return Task.FromResult(
            IsActivePermissionResult);
    }

    public Task<ProjectMemberPermission?> GetActiveForUpdateAsync(
        long projectMemberId,
        ProjectPermission permission,
        CancellationToken cancellationToken = default)
    {
        GetActiveForUpdateCalls.Add(
            (projectMemberId, permission));

        if (ActivePermissionsForUpdateToReturn.TryGetValue(
                (projectMemberId, permission),
                out var projectMemberPermission))
        {
            return Task.FromResult(
                projectMemberPermission);
        }

        return Task.FromResult(
            ActivePermissionForUpdateToReturn);
    }

    public Task<IReadOnlyCollection<ProjectMemberPermissionListItemData>>
        GetActivePermissionsAsync(
            long projectMemberId,
            CancellationToken cancellationToken = default)
    {
        LastGetActivePermissionsProjectMemberId =
            projectMemberId;

        return Task.FromResult(
            ActivePermissionsToReturn);
    }

    public Task AddAsync(
        ProjectMemberPermission projectMemberPermission,
        CancellationToken cancellationToken = default)
    {
        AddedProjectMemberPermission =
            projectMemberPermission;

        AddedProjectMemberPermissions.Add(
            projectMemberPermission);

        return Task.CompletedTask;
    }
}