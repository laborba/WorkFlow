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

    public async Task AddAsync(
        ProjectMember projectMember,
        CancellationToken cancellationToken = default)
    {
        await _context.ProjectMembers.AddAsync(
            projectMember,
            cancellationToken);
    }
}