using WorkFlow.Application.Abstractions.Persistence;
using WorkFlow.Application.Abstractions.Persistence.Models;
using WorkFlow.Application.Common.Pagination;
using WorkFlow.Domain.Entities;
using WorkFlow.Domain.Enums;

namespace WorkFlow.UnitTests.Application.Projects.Fakes;

internal sealed class FakeProjectRepository :
    IProjectRepository
{
    public Project? AddedProject { get; private set; }

    public Project? ProjectToReturn { get; set; }

    public PagedData<ProjectListItemData> PagedDataToReturn { get; set; } =
        new(
            Array.Empty<ProjectListItemData>(),
            0);

    public long? LastTenantId { get; private set; }

    public long? LastActiveMemberUserId { get; private set; }

    public int? LastPageNumber { get; private set; }

    public int? LastPageSize { get; private set; }

    public string? LastSearch { get; private set; }

    public ProjectStatus? LastStatus { get; private set; }

    public Guid? LastResponsibleUserPublicId { get; private set; }

    public Task<Project?> GetByPublicIdAsync(
        long tenantId,
        Guid publicId,
        CancellationToken cancellationToken = default)
    {
        if (ProjectToReturn is null)
            return Task.FromResult<Project?>(null);

        if (ProjectToReturn.TenantId != tenantId ||
            ProjectToReturn.PublicId != publicId)
        {
            return Task.FromResult<Project?>(null);
        }

        return Task.FromResult<Project?>(
            ProjectToReturn);
    }

    public Task<PagedData<ProjectListItemData>> GetPagedAsync(
        long tenantId,
        long? activeMemberUserId,
        int pageNumber,
        int pageSize,
        string? search,
        ProjectStatus? status,
        Guid? responsibleUserPublicId,
        CancellationToken cancellationToken = default)
    {
        LastTenantId = tenantId;
        LastActiveMemberUserId = activeMemberUserId;
        LastPageNumber = pageNumber;
        LastPageSize = pageSize;
        LastSearch = search;
        LastStatus = status;
        LastResponsibleUserPublicId =
            responsibleUserPublicId;

        return Task.FromResult(
            PagedDataToReturn);
    }

    public Task AddAsync(
        Project project,
        CancellationToken cancellationToken = default)
    {
        AddedProject = project;

        return Task.CompletedTask;
    }
}