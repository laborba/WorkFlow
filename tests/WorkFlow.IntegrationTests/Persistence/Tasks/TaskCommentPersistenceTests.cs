using Microsoft.EntityFrameworkCore;
using WorkFlow.Domain.Entities;
using WorkFlow.Domain.Enums;
using WorkFlow.IntegrationTests.Infrastructure;
using Xunit;

namespace WorkFlow.IntegrationTests.Persistence.Tasks;

[Collection(TestCollectionNames.PostgreSql)]
public sealed class TaskCommentPersistenceTests
{
    private readonly PostgreSqlTestDatabase _database;

    public TaskCommentPersistenceTests(
        PostgreSqlTestDatabase database)
    {
        _database = database;
    }

    [Fact]
    public async Task
        Add_ShouldPersistTaskComment_WhenDataIsValid()
    {
        await using var context =
            _database.CreateDbContext();

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var uniqueValue =
            Guid.NewGuid().ToString("N");

        var tenant = new Tenant(
            $"Tenant {uniqueValue}",
            $"REG-{uniqueValue}",
            $"tenant-{uniqueValue}@test.local");

        context.Tenants.Add(tenant);

        await context.SaveChangesAsync();

        var author = new User(
            tenant.Id,
            "Autor do Comentário",
            $"author-{uniqueValue}@test.local",
            "author-password-hash",
            UserRole.Member);

        context.Users.Add(author);

        await context.SaveChangesAsync();

        var project = new Project(
            tenant.Id,
            $"Projeto {uniqueValue}",
            author.Id);

        context.Projects.Add(project);

        await context.SaveChangesAsync();

        var projectTask = new ProjectTask(
            project.Id,
            $"Tarefa {uniqueValue}",
            ProjectTaskPriority.Medium,
            author.Id);

        context.ProjectTasks.Add(projectTask);

        await context.SaveChangesAsync();

        var comment = new TaskComment(
            projectTask.Id,
            author.Id,
            "Comentário da tarefa");

        context.TaskComments.Add(comment);

        await context.SaveChangesAsync();

        var commentId = comment.Id;

        context.ChangeTracker.Clear();

        var persistedComment =
            await context.TaskComments.SingleAsync(
                item => item.Id == commentId);

        Assert.Equal(
            projectTask.Id,
            persistedComment.TaskId);

        Assert.Equal(
            author.Id,
            persistedComment.AuthorUserId);

        Assert.Equal(
            "Comentário da tarefa",
            persistedComment.Content);

        Assert.False(
            persistedComment.IsDeleted);

        Assert.Null(
            persistedComment.UpdatedAt);

        Assert.Null(
            persistedComment.DeletedAt);

        Assert.Null(
            persistedComment.DeletedByUserId);

        Assert.NotEqual(
            default,
            persistedComment.CreatedAt);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
    Edit_ShouldPersistContentAndUpdatedAt_WhenWithinEditWindow()
    {
        await using var context =
            _database.CreateDbContext();

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var uniqueValue =
            Guid.NewGuid().ToString("N");

        var tenant = new Tenant(
            $"Tenant {uniqueValue}",
            $"REG-{uniqueValue}",
            $"tenant-{uniqueValue}@test.local");

        context.Tenants.Add(tenant);

        await context.SaveChangesAsync();

        var author = new User(
            tenant.Id,
            "Autor do Comentário",
            $"author-{uniqueValue}@test.local",
            "author-password-hash",
            UserRole.Member);

        context.Users.Add(author);

        await context.SaveChangesAsync();

        var project = new Project(
            tenant.Id,
            $"Projeto {uniqueValue}",
            author.Id);

        context.Projects.Add(project);

        await context.SaveChangesAsync();

        var projectTask = new ProjectTask(
            project.Id,
            $"Tarefa {uniqueValue}",
            ProjectTaskPriority.Medium,
            author.Id);

        context.ProjectTasks.Add(projectTask);

        await context.SaveChangesAsync();

        var comment = new TaskComment(
            projectTask.Id,
            author.Id,
            "Comentário original");

        context.TaskComments.Add(comment);

        await context.SaveChangesAsync();

        var commentId = comment.Id;

        context.ChangeTracker.Clear();

        var commentToEdit =
            await context.TaskComments.SingleAsync(
                item => item.Id == commentId);

        var editedAt =
            commentToEdit.CreatedAt.AddMinutes(5);

        commentToEdit.Edit(
            "Comentário atualizado",
            author.Id,
            editedAt);

        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var persistedComment =
            await context.TaskComments.SingleAsync(
                item => item.Id == commentId);

        Assert.Equal(
            "Comentário atualizado",
            persistedComment.Content);

        Assert.Equal(
            editedAt,
            persistedComment.UpdatedAt);

        Assert.False(
            persistedComment.IsDeleted);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
    Delete_ShouldPersistDeletion_WhenAuthorIsWithinEditWindow()
    {
        await using var context =
            _database.CreateDbContext();

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var uniqueValue =
            Guid.NewGuid().ToString("N");

        var tenant = new Tenant(
            $"Tenant {uniqueValue}",
            $"REG-{uniqueValue}",
            $"tenant-{uniqueValue}@test.local");

        context.Tenants.Add(tenant);

        await context.SaveChangesAsync();

        var author = new User(
            tenant.Id,
            "Autor do Comentário",
            $"author-{uniqueValue}@test.local",
            "author-password-hash",
            UserRole.Member);

        context.Users.Add(author);

        await context.SaveChangesAsync();

        var project = new Project(
            tenant.Id,
            $"Projeto {uniqueValue}",
            author.Id);

        context.Projects.Add(project);

        await context.SaveChangesAsync();

        var projectTask = new ProjectTask(
            project.Id,
            $"Tarefa {uniqueValue}",
            ProjectTaskPriority.Medium,
            author.Id);

        context.ProjectTasks.Add(projectTask);

        await context.SaveChangesAsync();

        var comment = new TaskComment(
            projectTask.Id,
            author.Id,
            "Comentário que será removido");

        context.TaskComments.Add(comment);

        await context.SaveChangesAsync();

        var commentId = comment.Id;

        context.ChangeTracker.Clear();

        var commentToDelete =
            await context.TaskComments.SingleAsync(
                item => item.Id == commentId);

        var deletedAt =
            commentToDelete.CreatedAt.AddMinutes(10);

        commentToDelete.Delete(
            author.Id,
            deletedAt);

        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var persistedComment =
            await context.TaskComments.SingleAsync(
                item => item.Id == commentId);

        Assert.True(
            persistedComment.IsDeleted);

        Assert.Equal(
            deletedAt,
            persistedComment.DeletedAt);

        Assert.Equal(
            author.Id,
            persistedComment.DeletedByUserId);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
    DeleteByModerator_ShouldPersistDeletion_AfterEditWindow()
    {
        await using var context =
            _database.CreateDbContext();

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var uniqueValue =
            Guid.NewGuid().ToString("N");

        var tenant = new Tenant(
            $"Tenant {uniqueValue}",
            $"REG-{uniqueValue}",
            $"tenant-{uniqueValue}@test.local");

        context.Tenants.Add(tenant);

        await context.SaveChangesAsync();

        var author = new User(
            tenant.Id,
            "Autor do Comentário",
            $"author-{uniqueValue}@test.local",
            "author-password-hash",
            UserRole.Member);

        var moderator = new User(
            tenant.Id,
            "Moderador do Projeto",
            $"moderator-{uniqueValue}@test.local",
            "moderator-password-hash",
            UserRole.ProjectManager);

        context.Users.AddRange(
            author,
            moderator);

        await context.SaveChangesAsync();

        var project = new Project(
            tenant.Id,
            $"Projeto {uniqueValue}",
            moderator.Id);

        context.Projects.Add(project);

        await context.SaveChangesAsync();

        var projectTask = new ProjectTask(
            project.Id,
            $"Tarefa {uniqueValue}",
            ProjectTaskPriority.Medium,
            moderator.Id);

        context.ProjectTasks.Add(projectTask);

        await context.SaveChangesAsync();

        var comment = new TaskComment(
            projectTask.Id,
            author.Id,
            "Comentário removido pelo moderador");

        context.TaskComments.Add(comment);

        await context.SaveChangesAsync();

        var commentId = comment.Id;

        context.ChangeTracker.Clear();

        var commentToDelete =
            await context.TaskComments.SingleAsync(
                item => item.Id == commentId);

        var deletedAt =
            commentToDelete.CreatedAt.AddMinutes(30);

        commentToDelete.DeleteByModerator(
            moderator.Id,
            deletedAt);

        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var persistedComment =
            await context.TaskComments.SingleAsync(
                item => item.Id == commentId);

        Assert.True(
            persistedComment.IsDeleted);

        Assert.Equal(
            deletedAt,
            persistedComment.DeletedAt);

        Assert.Equal(
            moderator.Id,
            persistedComment.DeletedByUserId);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
    SaveChanges_ShouldThrow_WhenTaskDoesNotExist()
    {
        await using var context =
            _database.CreateDbContext();

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var uniqueValue =
            Guid.NewGuid().ToString("N");

        var tenant = new Tenant(
            $"Tenant {uniqueValue}",
            $"REG-{uniqueValue}",
            $"tenant-{uniqueValue}@test.local");

        context.Tenants.Add(tenant);

        await context.SaveChangesAsync();

        var author = new User(
            tenant.Id,
            "Autor do Comentário",
            $"author-{uniqueValue}@test.local",
            "author-password-hash",
            UserRole.Member);

        context.Users.Add(author);

        await context.SaveChangesAsync();

        var comment = new TaskComment(
            long.MaxValue,
            author.Id,
            "Comentário sem tarefa existente");

        context.TaskComments.Add(comment);

        await Assert.ThrowsAsync<DbUpdateException>(
            () => context.SaveChangesAsync());

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
    SaveChanges_ShouldThrow_WhenAuthorDoesNotExist()
    {
        await using var context =
            _database.CreateDbContext();

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var uniqueValue =
            Guid.NewGuid().ToString("N");

        var tenant = new Tenant(
            $"Tenant {uniqueValue}",
            $"REG-{uniqueValue}",
            $"tenant-{uniqueValue}@test.local");

        context.Tenants.Add(tenant);

        await context.SaveChangesAsync();

        var creator = new User(
            tenant.Id,
            "Criador da Tarefa",
            $"creator-{uniqueValue}@test.local",
            "creator-password-hash",
            UserRole.ProjectManager);

        context.Users.Add(creator);

        await context.SaveChangesAsync();

        var project = new Project(
            tenant.Id,
            $"Projeto {uniqueValue}",
            creator.Id);

        context.Projects.Add(project);

        await context.SaveChangesAsync();

        var projectTask = new ProjectTask(
            project.Id,
            $"Tarefa {uniqueValue}",
            ProjectTaskPriority.Medium,
            creator.Id);

        context.ProjectTasks.Add(projectTask);

        await context.SaveChangesAsync();

        var comment = new TaskComment(
            projectTask.Id,
            long.MaxValue,
            "Comentário sem autor existente");

        context.TaskComments.Add(comment);

        await Assert.ThrowsAsync<DbUpdateException>(
            () => context.SaveChangesAsync());

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
    SaveChanges_ShouldThrow_WhenDeletedByUserDoesNotExist()
    {
        await using var context =
            _database.CreateDbContext();

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var uniqueValue =
            Guid.NewGuid().ToString("N");

        var tenant = new Tenant(
            $"Tenant {uniqueValue}",
            $"REG-{uniqueValue}",
            $"tenant-{uniqueValue}@test.local");

        context.Tenants.Add(tenant);

        await context.SaveChangesAsync();

        var author = new User(
            tenant.Id,
            "Autor do Comentário",
            $"author-{uniqueValue}@test.local",
            "author-password-hash",
            UserRole.Member);

        context.Users.Add(author);

        await context.SaveChangesAsync();

        var project = new Project(
            tenant.Id,
            $"Projeto {uniqueValue}",
            author.Id);

        context.Projects.Add(project);

        await context.SaveChangesAsync();

        var projectTask = new ProjectTask(
            project.Id,
            $"Tarefa {uniqueValue}",
            ProjectTaskPriority.Medium,
            author.Id);

        context.ProjectTasks.Add(projectTask);

        await context.SaveChangesAsync();

        var comment = new TaskComment(
            projectTask.Id,
            author.Id,
            "Comentário que será removido");

        context.TaskComments.Add(comment);

        await context.SaveChangesAsync();

        var deletedAt =
            comment.CreatedAt.AddMinutes(1);

        comment.DeleteByModerator(
            long.MaxValue,
            deletedAt);

        await Assert.ThrowsAsync<DbUpdateException>(
            () => context.SaveChangesAsync());

        await transaction.RollbackAsync();
    }
}