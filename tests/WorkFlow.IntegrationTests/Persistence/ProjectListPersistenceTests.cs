using WorkFlow.Domain.Entities;
using WorkFlow.Domain.Enums;
using WorkFlow.Infrastructure.Persistence.Repositories;
using WorkFlow.IntegrationTests.Infrastructure;
using Xunit;

namespace WorkFlow.IntegrationTests.Persistence;

[Collection(TestCollectionNames.PostgreSql)]
public sealed class ProjectListPersistenceTests
{
    private readonly PostgreSqlTestDatabase _database;

    public ProjectListPersistenceTests(
        PostgreSqlTestDatabase database)
    {
        _database = database;
    }

    [Fact]
    public async Task
    GetPagedAsync_ShouldExcludeArchivedAndOtherTenantProjects_ByDefault()
    {
        await using var context =
            _database.CreateDbContext();

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var uniqueValue =
            Guid.NewGuid().ToString("N");

        var tenant =
            new Tenant(
                $"Tenant A {uniqueValue}",
                $"REG-A-{uniqueValue}",
                $"tenant-a-{uniqueValue}@test.local");

        var otherTenant =
            new Tenant(
                $"Tenant B {uniqueValue}",
                $"REG-B-{uniqueValue}",
                $"tenant-b-{uniqueValue}@test.local");

        context.Tenants.AddRange(
            tenant,
            otherTenant);

        await context.SaveChangesAsync();

        var creator =
            new User(
                tenant.Id,
                "Criador A",
                $"creator-a-{uniqueValue}@test.local",
                "password-hash",
                UserRole.TenantAdmin);

        var otherCreator =
            new User(
                otherTenant.Id,
                "Criador B",
                $"creator-b-{uniqueValue}@test.local",
                "password-hash",
                UserRole.TenantAdmin);

        context.Users.AddRange(
            creator,
            otherCreator);

        await context.SaveChangesAsync();

        var visibleProject =
            new Project(
                tenant.Id,
                $"Projeto Visível {uniqueValue}",
                creator.Id);

        var archivedProject =
            new Project(
                tenant.Id,
                $"Projeto Arquivado {uniqueValue}",
                creator.Id);

        archivedProject.Archive();

        var otherTenantProject =
            new Project(
                otherTenant.Id,
                $"Projeto Outro Tenant {uniqueValue}",
                otherCreator.Id);

        context.Projects.AddRange(
            visibleProject,
            archivedProject,
            otherTenantProject);

        await context.SaveChangesAsync();

        var repository =
            new ProjectRepository(context);

        var result =
            await repository.GetPagedAsync(
                tenant.Id,
                null,
                1,
                20,
                null,
                null,
                null);

        Assert.Equal(
            1,
            result.TotalCount);

        var item =
            Assert.Single(result.Items);

        Assert.Equal(
            visibleProject.PublicId,
            item.PublicId);

        Assert.Equal(
            ProjectStatus.Planning,
            item.Status);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
    GetPagedAsync_ShouldReturnArchivedProjects_WhenArchivedStatusIsRequested()
    {
        await using var context =
            _database.CreateDbContext();

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var uniqueValue =
            Guid.NewGuid().ToString("N");

        var tenant =
            new Tenant(
                $"Tenant {uniqueValue}",
                $"REG-{uniqueValue}",
                $"tenant-{uniqueValue}@test.local");

        context.Tenants.Add(
            tenant);

        await context.SaveChangesAsync();

        var creator =
            new User(
                tenant.Id,
                "Criador",
                $"creator-{uniqueValue}@test.local",
                "password-hash",
                UserRole.TenantAdmin);

        context.Users.Add(
            creator);

        await context.SaveChangesAsync();

        var activeProject =
            new Project(
                tenant.Id,
                $"Projeto Ativo {uniqueValue}",
                creator.Id);

        var archivedProject =
            new Project(
                tenant.Id,
                $"Projeto Arquivado {uniqueValue}",
                creator.Id);

        archivedProject.Archive();

        context.Projects.AddRange(
            activeProject,
            archivedProject);

        await context.SaveChangesAsync();

        var repository =
            new ProjectRepository(context);

        var result =
            await repository.GetPagedAsync(
                tenant.Id,
                null,
                1,
                20,
                null,
                ProjectStatus.Archived,
                null);

        Assert.Equal(
            1,
            result.TotalCount);

        var item =
            Assert.Single(result.Items);

        Assert.Equal(
            archivedProject.PublicId,
            item.PublicId);

        Assert.Equal(
            ProjectStatus.Archived,
            item.Status);

        Assert.NotNull(
            item.ArchivedAt);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
    GetPagedAsync_ShouldReturnOnlyProjectsWithActiveMembership()
    {
        await using var context =
            _database.CreateDbContext();

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var uniqueValue =
            Guid.NewGuid().ToString("N");

        var tenant =
            new Tenant(
                $"Tenant {uniqueValue}",
                $"REG-{uniqueValue}",
                $"tenant-{uniqueValue}@test.local");

        context.Tenants.Add(
            tenant);

        await context.SaveChangesAsync();

        var creator =
            new User(
                tenant.Id,
                "Criador",
                $"creator-{uniqueValue}@test.local",
                "password-hash",
                UserRole.ProjectManager);

        var member =
            new User(
                tenant.Id,
                "Membro",
                $"member-{uniqueValue}@test.local",
                "password-hash",
                UserRole.Member);

        context.Users.AddRange(
            creator,
            member);

        await context.SaveChangesAsync();

        var activeMembershipProject =
            new Project(
                tenant.Id,
                $"Projeto Ativo {uniqueValue}",
                creator.Id);

        var removedMembershipProject =
            new Project(
                tenant.Id,
                $"Projeto Removido {uniqueValue}",
                creator.Id);

        var unrelatedProject =
            new Project(
                tenant.Id,
                $"Projeto Sem Participação {uniqueValue}",
                creator.Id);

        context.Projects.AddRange(
            activeMembershipProject,
            removedMembershipProject,
            unrelatedProject);

        await context.SaveChangesAsync();

        var activeMembership =
            new ProjectMember(
                activeMembershipProject.Id,
                member.Id,
                creator.Id);

        var removedMembership =
            new ProjectMember(
                removedMembershipProject.Id,
                member.Id,
                creator.Id);

        removedMembership.Remove();

        context.ProjectMembers.AddRange(
            activeMembership,
            removedMembership);

        await context.SaveChangesAsync();

        var repository =
            new ProjectRepository(context);

        var result =
            await repository.GetPagedAsync(
                tenant.Id,
                member.Id,
                1,
                20,
                null,
                null,
                null);

        Assert.Equal(
            1,
            result.TotalCount);

        var item =
            Assert.Single(result.Items);

        Assert.Equal(
            activeMembershipProject.PublicId,
            item.PublicId);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
    GetPagedAsync_ShouldSearchByNameOrDescription_CaseInsensitive()
    {
        await using var context =
            _database.CreateDbContext();

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var uniqueValue =
            Guid.NewGuid().ToString("N");

        var tenant =
            new Tenant(
                $"Tenant {uniqueValue}",
                $"REG-{uniqueValue}",
                $"tenant-{uniqueValue}@test.local");

        context.Tenants.Add(
            tenant);

        await context.SaveChangesAsync();

        var creator =
            new User(
                tenant.Id,
                "Criador",
                $"creator-{uniqueValue}@test.local",
                "password-hash",
                UserRole.TenantAdmin);

        context.Users.Add(
            creator);

        await context.SaveChangesAsync();

        var projectByName =
            new Project(
                tenant.Id,
                $"Projeto CLIENTE {uniqueValue}",
                creator.Id,
                "Descrição comum");

        var projectByDescription =
            new Project(
                tenant.Id,
                $"Projeto Interno {uniqueValue}",
                creator.Id,
                "Integração com Cliente externo");

        var unrelatedProject =
            new Project(
                tenant.Id,
                $"Projeto Financeiro {uniqueValue}",
                creator.Id,
                "Processamento interno");

        context.Projects.AddRange(
            projectByName,
            projectByDescription,
            unrelatedProject);

        await context.SaveChangesAsync();

        var repository =
            new ProjectRepository(context);

        var result =
            await repository.GetPagedAsync(
                tenant.Id,
                null,
                1,
                20,
                "cliente",
                null,
                null);

        Assert.Equal(
            2,
            result.TotalCount);

        var publicIds =
            result.Items
                .Select(item => item.PublicId)
                .ToArray();

        Assert.Contains(
            projectByName.PublicId,
            publicIds);

        Assert.Contains(
            projectByDescription.PublicId,
            publicIds);

        Assert.DoesNotContain(
            unrelatedProject.PublicId,
            publicIds);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
    GetPagedAsync_ShouldFilterByResponsibleAndReturnResponsibleData()
    {
        await using var context =
            _database.CreateDbContext();

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var uniqueValue =
            Guid.NewGuid().ToString("N");

        var tenant =
            new Tenant(
                $"Tenant {uniqueValue}",
                $"REG-{uniqueValue}",
                $"tenant-{uniqueValue}@test.local");

        context.Tenants.Add(
            tenant);

        await context.SaveChangesAsync();

        var creator =
            new User(
                tenant.Id,
                "Criador",
                $"creator-{uniqueValue}@test.local",
                "password-hash",
                UserRole.TenantAdmin);

        var responsible =
            new User(
                tenant.Id,
                "Responsável Principal",
                $"responsible-{uniqueValue}@test.local",
                "password-hash",
                UserRole.Member);

        var otherResponsible =
            new User(
                tenant.Id,
                "Outro Responsável",
                $"other-responsible-{uniqueValue}@test.local",
                "password-hash",
                UserRole.Member);

        context.Users.AddRange(
            creator,
            responsible,
            otherResponsible);

        await context.SaveChangesAsync();

        var matchingProject =
            new Project(
                tenant.Id,
                $"Projeto Principal {uniqueValue}",
                creator.Id,
                responsibleUserId: responsible.Id);

        var otherProject =
            new Project(
                tenant.Id,
                $"Outro Projeto {uniqueValue}",
                creator.Id,
                responsibleUserId: otherResponsible.Id);

        var projectWithoutResponsible =
            new Project(
                tenant.Id,
                $"Projeto Sem Responsável {uniqueValue}",
                creator.Id);

        context.Projects.AddRange(
            matchingProject,
            otherProject,
            projectWithoutResponsible);

        await context.SaveChangesAsync();

        var repository =
            new ProjectRepository(context);

        var result =
            await repository.GetPagedAsync(
                tenant.Id,
                null,
                1,
                20,
                null,
                null,
                responsible.PublicId);

        Assert.Equal(
            1,
            result.TotalCount);

        var item =
            Assert.Single(result.Items);

        Assert.Equal(
            matchingProject.PublicId,
            item.PublicId);

        Assert.True(
            item.HasResponsibleUser);

        Assert.Equal(
            responsible.PublicId,
            item.ResponsibleUserPublicId);

        Assert.Equal(
            responsible.Name,
            item.ResponsibleUserName);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
    GetPagedAsync_ShouldApplyPaginationAndReturnCorrectTotalCount()
    {
        await using var context =
            _database.CreateDbContext();

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var uniqueValue =
            Guid.NewGuid().ToString("N");

        var tenant =
            new Tenant(
                $"Tenant {uniqueValue}",
                $"REG-{uniqueValue}",
                $"tenant-{uniqueValue}@test.local");

        context.Tenants.Add(
            tenant);

        await context.SaveChangesAsync();

        var creator =
            new User(
                tenant.Id,
                "Criador",
                $"creator-{uniqueValue}@test.local",
                "password-hash",
                UserRole.TenantAdmin);

        context.Users.Add(
            creator);

        await context.SaveChangesAsync();

        var project1 =
            new Project(
                tenant.Id,
                $"Projeto 1 {uniqueValue}",
                creator.Id);

        var project2 =
            new Project(
                tenant.Id,
                $"Projeto 2 {uniqueValue}",
                creator.Id);

        var project3 =
            new Project(
                tenant.Id,
                $"Projeto 3 {uniqueValue}",
                creator.Id);

        context.Projects.AddRange(
            project1,
            project2,
            project3);

        await context.SaveChangesAsync();

        var repository =
            new ProjectRepository(context);

        var firstPage =
            await repository.GetPagedAsync(
                tenant.Id,
                null,
                1,
                2,
                null,
                null,
                null);

        var secondPage =
            await repository.GetPagedAsync(
                tenant.Id,
                null,
                2,
                2,
                null,
                null,
                null);

        Assert.Equal(
            3,
            firstPage.TotalCount);

        Assert.Equal(
            3,
            secondPage.TotalCount);

        Assert.Equal(
            2,
            firstPage.Items.Count);

        Assert.Single(
            secondPage.Items);

        var allPublicIds =
            firstPage.Items
                .Concat(secondPage.Items)
                .Select(item => item.PublicId)
                .ToArray();

        Assert.Equal(
            3,
            allPublicIds.Distinct().Count());

        Assert.Contains(
            project1.PublicId,
            allPublicIds);

        Assert.Contains(
            project2.PublicId,
            allPublicIds);

        Assert.Contains(
            project3.PublicId,
            allPublicIds);

        await transaction.RollbackAsync();
    }
}