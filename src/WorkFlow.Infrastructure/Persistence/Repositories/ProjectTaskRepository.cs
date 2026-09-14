using Microsoft.EntityFrameworkCore;
using WorkFlow.Application.Abstractions.Persistence;
using WorkFlow.Domain.Entities;
using WorkFlow.Domain.Enums;

namespace WorkFlow.Infrastructure.Persistence.Repositories;

public sealed class ProjectTaskRepository :
    IProjectTaskRepository
{
    private readonly WorkFlowDbContext _context;

    public ProjectTaskRepository(
        WorkFlowDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyCollection<ProjectTaskStatus>>
        GetStatusesByProjectIdAsync(
            long projectId,
            CancellationToken cancellationToken = default)
    {
        return await _context.ProjectTasks
            .AsNoTracking()
            .Where(task =>
                task.ProjectId == projectId)
            .Select(task =>
                task.Status)
            .ToArrayAsync(
                cancellationToken);
    }

    public async Task AddAsync(
        ProjectTask projectTask,
        CancellationToken cancellationToken = default)
    {
        await _context.ProjectTasks.AddAsync(
            projectTask,
            cancellationToken);
    }
}