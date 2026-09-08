using Microsoft.EntityFrameworkCore;
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

    public async Task<Project?> GetByPublicIdAsync(
        long tenantId,
        Guid publicId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Projects
            .AsNoTracking()
            .SingleOrDefaultAsync(
                project =>
                    project.TenantId == tenantId &&
                    project.PublicId == publicId,
                cancellationToken);
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