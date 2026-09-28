using WorkFlow.Application.Abstractions.Persistence;
using WorkFlow.Application.Common.Results;
using WorkFlow.Application.Projects;
using WorkFlow.Application.Tenants;
using WorkFlow.Application.Users;
using WorkFlow.Domain.Enums;

namespace WorkFlow.Application.ProjectTasks.ListProjectTasks;

public sealed class ListProjectTasksHandler
{
    private const int MaximumPageSize = 100;

    private readonly ITenantRepository _tenantRepository;
    private readonly IUserRepository _userRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IProjectMemberRepository _projectMemberRepository;
    private readonly IProjectTaskRepository _projectTaskRepository;

    public ListProjectTasksHandler(
        ITenantRepository tenantRepository,
        IUserRepository userRepository,
        IProjectRepository projectRepository,
        IProjectMemberRepository projectMemberRepository,
        IProjectTaskRepository projectTaskRepository)
    {
        _tenantRepository = tenantRepository;
        _userRepository = userRepository;
        _projectRepository = projectRepository;
        _projectMemberRepository = projectMemberRepository;
        _projectTaskRepository = projectTaskRepository;
    }

    public async Task<Result<ListProjectTasksResult>> HandleAsync(
        ListProjectTasksQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.TenantPublicId == Guid.Empty)
        {
            throw new ArgumentException(
                "O identificador público da empresa não pode estar vazio.",
                nameof(query.TenantPublicId));
        }

        if (query.ProjectPublicId == Guid.Empty)
        {
            throw new ArgumentException(
                "O identificador público do projeto não pode estar vazio.",
                nameof(query.ProjectPublicId));
        }

        if (query.RequestedByUserPublicId == Guid.Empty)
        {
            throw new ArgumentException(
                "O identificador público do usuário solicitante não pode estar vazio.",
                nameof(query.RequestedByUserPublicId));
        }

        if (query.PageNumber < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(query.PageNumber),
                query.PageNumber,
                "O número da página deve ser maior ou igual a 1.");
        }

        if (query.PageSize < 1 ||
            query.PageSize > MaximumPageSize)
        {
            throw new ArgumentOutOfRangeException(
                nameof(query.PageSize),
                query.PageSize,
                $"O tamanho da página deve estar entre 1 e {MaximumPageSize}.");
        }

        if (query.Status.HasValue &&
            !Enum.IsDefined(query.Status.Value))
        {
            throw new ArgumentException(
                "O status da tarefa é inválido.",
                nameof(query.Status));
        }

        if (query.Priority.HasValue &&
            !Enum.IsDefined(query.Priority.Value))
        {
            throw new ArgumentException(
                "A prioridade da tarefa é inválida.",
                nameof(query.Priority));
        }

        if (query.ResponsibleUserPublicId.HasValue &&
            query.ResponsibleUserPublicId.Value == Guid.Empty)
        {
            throw new ArgumentException(
                "O identificador público do responsável não pode estar vazio.",
                nameof(query.ResponsibleUserPublicId));
        }

        var tenant =
            await _tenantRepository.GetByPublicIdAsync(
                query.TenantPublicId,
                cancellationToken);

        if (tenant is null)
        {
            return Result<ListProjectTasksResult>.Failure(
                TenantErrors.NotFound);
        }

        if (!tenant.IsActive)
        {
            return Result<ListProjectTasksResult>.Failure(
                TenantErrors.Inactive);
        }

        var requestedByUser =
            await _userRepository.GetByPublicIdAsync(
                tenant.Id,
                query.RequestedByUserPublicId,
                cancellationToken);

        if (requestedByUser is null)
        {
            return Result<ListProjectTasksResult>.Failure(
                UserErrors.NotFound);
        }

        if (!requestedByUser.IsActive)
        {
            return Result<ListProjectTasksResult>.Failure(
                UserErrors.Inactive);
        }

        var project =
            await _projectRepository.GetByPublicIdAsync(
                tenant.Id,
                query.ProjectPublicId,
                cancellationToken);

        if (project is null)
        {
            return Result<ListProjectTasksResult>.Failure(
                ProjectErrors.NotFound);
        }

        if (requestedByUser.Role != UserRole.TenantAdmin)
        {
            if (requestedByUser.Role != UserRole.ProjectManager &&
                requestedByUser.Role != UserRole.Member)
            {
                return Result<ListProjectTasksResult>.Failure(
                    ProjectErrors.ViewNotAllowed);
            }

            var isActiveMember =
                await _projectMemberRepository.IsActiveMemberAsync(
                    project.Id,
                    requestedByUser.Id,
                    cancellationToken);

            if (!isActiveMember)
            {
                return Result<ListProjectTasksResult>.Failure(
                    ProjectErrors.ViewNotAllowed);
            }
        }

        var pagedData =
            await _projectTaskRepository.GetPagedAsync(
                tenant.Id,
                project.Id,
                query.PageNumber,
                query.PageSize,
                query.Search,
                query.Status,
                query.Priority,
                query.ResponsibleUserPublicId,
                query.IsArchived,
                cancellationToken);

        var items =
            pagedData.Items
                .Select(task =>
                {
                    if (!task.CreatedByUserPublicId.HasValue)
                    {
                        throw new InvalidOperationException(
                            "O usuário criador associado à tarefa não foi encontrado.");
                    }

                    if (task.HasResponsibleUser &&
                        (!task.ResponsibleUserPublicId.HasValue ||
                         task.ResponsibleUserName is null))
                    {
                        throw new InvalidOperationException(
                            "O usuário responsável associado à tarefa não foi encontrado.");
                    }

                    if (task.HasValidatorUser &&
                        !task.ValidatorUserPublicId.HasValue)
                    {
                        throw new InvalidOperationException(
                            "O usuário validador associado à tarefa não foi encontrado.");
                    }

                    return new ProjectTaskListItemResult(
                        task.PublicId,
                        task.CreatedByUserPublicId.Value,
                        task.ResponsibleUserPublicId,
                        task.ResponsibleUserName,
                        task.ValidatorUserPublicId,
                        task.Title,
                        task.Description,
                        task.Status,
                        task.Priority,
                        task.DueDate,
                        task.CreatedAt,
                        task.UpdatedAt,
                        task.ArchivedAt);
                })
                .ToArray();

        var totalPages =
            (int)Math.Ceiling(
                pagedData.TotalCount /
                (double)query.PageSize);

        return Result<ListProjectTasksResult>.Success(
            new ListProjectTasksResult(
                items,
                query.PageNumber,
                query.PageSize,
                pagedData.TotalCount,
                totalPages));
    }
}