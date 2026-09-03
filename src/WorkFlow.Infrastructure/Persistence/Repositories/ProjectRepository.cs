using WorkFlow.Application.Abstractions.Persistence;
using WorkFlow.Domain.Entities;

namespace WorkFlow.Infrastructure.Persistence.Repositories;

public sealed class ProjectRepository : IProjectRepository
{
    private readonly WorkFlowDbContext _context;

    public ProjectRepository(
        WorkFlowDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(
        Project project,
        CancellationToken cancellationToken = default)
    {
        await _context.Projects.AddAsync(
            project,
            cancellationToken);
    }
}