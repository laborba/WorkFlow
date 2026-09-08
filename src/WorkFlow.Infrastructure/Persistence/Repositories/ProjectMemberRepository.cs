using Microsoft.EntityFrameworkCore;
using WorkFlow.Application.Abstractions.Persistence;
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

    public async Task AddAsync(
        ProjectMember projectMember,
        CancellationToken cancellationToken = default)
    {
        await _context.ProjectMembers.AddAsync(
            projectMember,
            cancellationToken);
    }
}