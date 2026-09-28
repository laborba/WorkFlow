using Microsoft.EntityFrameworkCore;
using WorkFlow.Application.Abstractions.Persistence;
using WorkFlow.Application.Abstractions.Persistence.Models;
using WorkFlow.Application.Common.Pagination;
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

    public async Task<PagedData<ProjectTaskListItemData>> GetPagedAsync(
        long tenantId,
        long projectId,
        int pageNumber,
        int pageSize,
        string? search,
        ProjectTaskStatus? status,
        ProjectTaskPriority? priority,
        Guid? responsibleUserPublicId,
        bool? isArchived,
        CancellationToken cancellationToken = default)
    {
        var users =
            _context.Users
                .AsNoTracking()
                .Where(user =>
                    user.TenantId == tenantId);

        var projects =
            _context.Projects
                .AsNoTracking()
                .Where(project =>
                    project.TenantId == tenantId &&
                    project.Id == projectId);

        var query =
            from task in _context.ProjectTasks.AsNoTracking()

            join project in projects
                on task.ProjectId equals project.Id

            join createdByUser in users
                on task.CreatedByUserId equals createdByUser.Id
                into createdByUsers

            from createdByUser in createdByUsers.DefaultIfEmpty()

            join responsibleUser in users
                on task.ResponsibleUserId equals (long?)responsibleUser.Id
                into responsibleUsers

            from responsibleUser in responsibleUsers.DefaultIfEmpty()

            join validatorUser in users
                on task.ValidatorUserId equals (long?)validatorUser.Id
                into validatorUsers

            from validatorUser in validatorUsers.DefaultIfEmpty()

            select new
            {
                Task = task,
                CreatedByUser = createdByUser,
                ResponsibleUser = responsibleUser,
                ValidatorUser = validatorUser
            };

        if (status.HasValue)
        {
            query =
                query.Where(item =>
                    item.Task.Status == status.Value);
        }

        if (priority.HasValue)
        {
            query =
                query.Where(item =>
                    item.Task.Priority == priority.Value);
        }

        if (isArchived == true)
        {
            query =
                query.Where(item =>
                    item.Task.ArchivedAt != null);
        }
        else
        {
            query =
                query.Where(item =>
                    item.Task.ArchivedAt == null);
        }

        if (responsibleUserPublicId.HasValue)
        {
            var responsiblePublicId =
                responsibleUserPublicId.Value;

            query =
                query.Where(item =>
                    item.ResponsibleUser != null &&
                    item.ResponsibleUser.PublicId ==
                        responsiblePublicId);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchPattern =
                $"%{search.Trim()}%";

            query =
                query.Where(item =>
                    EF.Functions.ILike(
                        item.Task.Title,
                        searchPattern) ||
                    (
                        item.Task.Description != null &&
                        EF.Functions.ILike(
                            item.Task.Description,
                            searchPattern)
                    ));
        }

        var totalCount =
            await query.CountAsync(
                cancellationToken);

        var items =
            await query
                .OrderByDescending(item =>
                    item.Task.CreatedAt)
                .ThenByDescending(item =>
                    item.Task.Id)
                .Skip(
                    (pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(item =>
                    new ProjectTaskListItemData(
                        item.Task.PublicId,
                        item.CreatedByUser == null
                            ? null
                            : (Guid?)item.CreatedByUser.PublicId,
                        item.Task.ResponsibleUserId.HasValue,
                        item.ResponsibleUser == null
                            ? null
                            : (Guid?)item.ResponsibleUser.PublicId,
                        item.ResponsibleUser == null
                            ? null
                            : item.ResponsibleUser.Name,
                        item.Task.ValidatorUserId.HasValue,
                        item.ValidatorUser == null
                            ? null
                            : (Guid?)item.ValidatorUser.PublicId,
                        item.Task.Title,
                        item.Task.Description,
                        item.Task.Status,
                        item.Task.Priority,
                        item.Task.DueDate,
                        item.Task.CreatedAt,
                        item.Task.UpdatedAt,
                        item.Task.ArchivedAt))
                .ToListAsync(
                    cancellationToken);

        return new PagedData<ProjectTaskListItemData>(
            items,
            totalCount);
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