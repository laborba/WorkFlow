using Microsoft.EntityFrameworkCore;
using WorkFlow.Domain.Entities;

namespace WorkFlow.Infrastructure.Persistence;

public sealed class WorkFlowDbContext : DbContext
{
    public WorkFlowDbContext(
        DbContextOptions<WorkFlowDbContext> options)
        : base(options)
    {
    }

    public DbSet<Tenant> Tenants =>
    Set<Tenant>();

    public DbSet<User> Users =>
        Set<User>();

    public DbSet<Project> Projects =>
        Set<Project>();

    public DbSet<ProjectMember> ProjectMembers =>
        Set<ProjectMember>();

    public DbSet<ProjectMemberPermission> ProjectMemberPermissions =>
        Set<ProjectMemberPermission>();

    public DbSet<ProjectTask> ProjectTasks =>
        Set<ProjectTask>();

    public DbSet<TaskCollaborator> TaskCollaborators =>
        Set<TaskCollaborator>();

    public DbSet<TaskComment> TaskComments =>
        Set<TaskComment>();

    public DbSet<ChatMessage> ChatMessages =>
        Set<ChatMessage>();

    public DbSet<Notification> Notifications =>
        Set<Notification>();

    public DbSet<TaskHistory> TaskHistories =>
        Set<TaskHistory>();

    public DbSet<ProjectHistory> ProjectHistories =>
        Set<ProjectHistory>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {

        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(WorkFlowDbContext).Assembly);
    }
}