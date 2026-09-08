using Microsoft.EntityFrameworkCore;
using WorkFlow.Application.Abstractions.Persistence;
using WorkFlow.Application.Abstractions.Persistence.Models;
using WorkFlow.Application.Common.Pagination;
using WorkFlow.Domain.Entities;
using WorkFlow.Domain.Enums;

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

    public async Task<PagedData<ProjectListItemData>> GetPagedAsync(
        long tenantId,
        long? activeMemberUserId,
        int pageNumber,
        int pageSize,
        string? search,
        ProjectStatus? status,
        Guid? responsibleUserPublicId,
        CancellationToken cancellationToken = default)
    {
        var query =
            from project in
                _context.Projects.AsNoTracking()
            join responsibleUser in
                _context.Users
                    .AsNoTracking()
                    .Where(user =>
                        user.TenantId == tenantId)
                on project.ResponsibleUserId
                equals (long?)responsibleUser.Id
                into responsibleUsers
            from responsibleUser in
                responsibleUsers.DefaultIfEmpty()
            where project.TenantId == tenantId
            select new
            {
                Project = project,
                ResponsibleUser = responsibleUser
            };

        if (activeMemberUserId.HasValue)
        {
            var userId =
                activeMemberUserId.Value;

            query =
                query.Where(item =>
                    _context.ProjectMembers.Any(
                        projectMember =>
                            projectMember.ProjectId ==
                                item.Project.Id &&
                            projectMember.UserId ==
                                userId &&
                            projectMember.RemovedAt ==
                                null));
        }

        if (status.HasValue)
        {
            query =
                query.Where(item =>
                    item.Project.Status ==
                    status.Value);
        }
        else
        {
            query =
                query.Where(item =>
                    item.Project.Status !=
                    ProjectStatus.Archived);
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
                        item.Project.Name,
                        searchPattern) ||
                    (
                        item.Project.Description != null &&
                        EF.Functions.ILike(
                            item.Project.Description,
                            searchPattern)
                    ));
        }

        var totalCount =
            await query.CountAsync(
                cancellationToken);

        var items =
            await query
                .OrderByDescending(item =>
                    item.Project.CreatedAt)
                .ThenByDescending(item =>
                    item.Project.Id)
                .Skip(
                    (pageNumber - 1) *
                    pageSize)
                .Take(pageSize)
                .Select(item =>
                    new ProjectListItemData(
                        item.Project.PublicId,
                        item.Project.ResponsibleUserId.HasValue,
                        item.ResponsibleUser == null
                            ? null
                            : (Guid?)item.ResponsibleUser.PublicId,
                        item.ResponsibleUser == null
                            ? null
                            : item.ResponsibleUser.Name,
                        item.Project.Name,
                        item.Project.Description,
                        item.Project.Status,
                        item.Project.DueDate,
                        item.Project.CreatedAt,
                        item.Project.UpdatedAt,
                        item.Project.ArchivedAt))
                .ToListAsync(
                    cancellationToken);

        return new PagedData<ProjectListItemData>(
            items,
            totalCount);
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