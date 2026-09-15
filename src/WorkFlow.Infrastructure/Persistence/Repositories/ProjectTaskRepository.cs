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

    public async Task<ProjectTask?> GetForUpdateByPublicIdAsync(
        long projectId,
        Guid publicId,
        CancellationToken cancellationToken = default)
    {
        if (_context.Database.CurrentTransaction is null)
        {
            throw new InvalidOperationException(
                "A leitura de tarefa com bloqueio exige uma transação ativa.");
        }

        var projectTasks =
            await _context.ProjectTasks
                .FromSqlInterpolated(
                    $"""
                    SELECT *
                    FROM project_tasks
                    WHERE project_id = {projectId}
                      AND public_id = {publicId}
                    FOR UPDATE
                    """)
                .AsTracking()
                .ToListAsync(
                    cancellationToken);

        return projectTasks.SingleOrDefault();
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