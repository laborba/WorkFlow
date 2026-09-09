using Microsoft.EntityFrameworkCore;
using WorkFlow.Application.Abstractions.Persistence;
using WorkFlow.Application.Abstractions.Persistence.Models;
using WorkFlow.Application.Common.Pagination;
using WorkFlow.Domain.Entities;

namespace WorkFlow.Infrastructure.Persistence.Repositories;

public sealed class ProjectMemberRepository :
    IProjectMemberRepository
{
    private readonly WorkFlowDbContext _context;

    public ProjectMemberRepository(
        WorkFlowDbContext context)
    {
        _context = context;
    }

    public async Task<bool> IsActiveMemberAsync(
        long projectId,
        long userId,
        CancellationToken cancellationToken = default)
    {
        return await _context.ProjectMembers
            .AsNoTracking()
            .AnyAsync(
                projectMember =>
                    projectMember.ProjectId == projectId &&
                    projectMember.UserId == userId &&
                    projectMember.RemovedAt == null,
                cancellationToken);
    }

    public async Task<ProjectMember?> GetActiveAsync(
        long projectId,
        long userId,
        CancellationToken cancellationToken = default)
    {
        return await _context.ProjectMembers
            .AsNoTracking()
            .SingleOrDefaultAsync(
                projectMember =>
                    projectMember.ProjectId == projectId &&
                    projectMember.UserId == userId &&
                    projectMember.RemovedAt == null,
                cancellationToken);
    }

    public async Task<ProjectMember?> GetActiveForUpdateAsync(
        long projectId,
        long userId,
        CancellationToken cancellationToken = default)
    {
        return await _context.ProjectMembers
            .SingleOrDefaultAsync(
                projectMember =>
                    projectMember.ProjectId == projectId &&
                    projectMember.UserId == userId &&
                    projectMember.RemovedAt == null,
                cancellationToken);
    }

    public async Task<PagedData<ProjectMemberListItemData>> GetPagedAsync(
        long projectId,
        int pageNumber,
        int pageSize,
        string? search,
        CancellationToken cancellationToken = default)
    {
        var query =
            from projectMember in
                _context.ProjectMembers.AsNoTracking()
            join user in
                _context.Users.AsNoTracking()
                on projectMember.UserId equals user.Id
            join addedByUser in
                _context.Users.AsNoTracking()
                on projectMember.AddedByUserId equals addedByUser.Id
            where
                projectMember.ProjectId == projectId &&
                projectMember.RemovedAt == null
            select new
            {
                ProjectMember = projectMember,
                User = user,
                AddedByUser = addedByUser
            };

        if (!string.IsNullOrWhiteSpace(
                search))
        {
            var normalizedSearch =
                search.Trim();

            query =
                query.Where(item =>
                    EF.Functions.ILike(
                        item.User.Name,
                        $"%{normalizedSearch}%") ||
                    EF.Functions.ILike(
                        item.User.Email,
                        $"%{normalizedSearch}%"));
        }

        var totalCount =
            await query.CountAsync(
                cancellationToken);

        var items =
            await query
                .OrderBy(item =>
                    item.ProjectMember.AddedAt)
                .ThenBy(item =>
                    item.ProjectMember.Id)
                .Skip(
                    (pageNumber - 1) *
                    pageSize)
                .Take(
                    pageSize)
                .Select(item =>
                    new ProjectMemberListItemData(
                        item.User.PublicId,
                        item.User.Name,
                        item.User.Email,
                        item.User.Role,
                        item.ProjectMember.AddedAt,
                        item.AddedByUser.PublicId))
                .ToArrayAsync(
                    cancellationToken);

        return new PagedData<ProjectMemberListItemData>(
            items,
            totalCount);
    }

    public async Task AddAsync(
        ProjectMember projectMember,
        CancellationToken cancellationToken = default)
    {
        await _context.ProjectMembers.AddAsync(
            projectMember,
            cancellationToken);
    }
}