using WorkFlow.Application.Abstractions.Persistence;
using WorkFlow.Domain.Entities;

namespace WorkFlow.UnitTests.Application.Projects.Fakes;

internal sealed class FakeProjectRepository :
    IProjectRepository
{
    public Project? AddedProject { get; private set; }

    public Project? ProjectToReturn { get; set; }

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

        return Task.FromResult<Project?>(ProjectToReturn);
    }

    public Task AddAsync(
        Project project,
        CancellationToken cancellationToken = default)
    {
        AddedProject = project;

        return Task.CompletedTask;
    }
}