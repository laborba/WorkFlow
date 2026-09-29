using WorkFlow.Application.Abstractions.Persistence;
using WorkFlow.Application.Common.Results;
using WorkFlow.Application.Projects;
using WorkFlow.Application.Tenants;
using WorkFlow.Application.Users;
using WorkFlow.Domain.Enums;

namespace WorkFlow.Application.ProjectTasks.StartProjectTask;

public sealed class StartProjectTaskHandler
{
    private readonly ITenantRepository
        _tenantRepository;

    private readonly IUserRepository
        _userRepository;

    private readonly IProjectRepository
        _projectRepository;

    private readonly IProjectMemberRepository
        _projectMemberRepository;

    private readonly IProjectTaskRepository
        _projectTaskRepository;

    private readonly IUnitOfWork
        _unitOfWork;

    public StartProjectTaskHandler(
        ITenantRepository tenantRepository,
        IUserRepository userRepository,
        IProjectRepository projectRepository,
        IProjectMemberRepository projectMemberRepository,
        IProjectTaskRepository projectTaskRepository,
        IUnitOfWork unitOfWork)
    {
        _tenantRepository =
            tenantRepository;

        _userRepository =
            userRepository;

        _projectRepository =
            projectRepository;

        _projectMemberRepository =
            projectMemberRepository;

        _projectTaskRepository =
            projectTaskRepository;

        _unitOfWork =
            unitOfWork;
    }

    public async Task<Result<StartProjectTaskResult>>
        HandleAsync(
            StartProjectTaskCommand command,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            command);

        ValidatePublicIds(
            command);

        var tenant =
            await _tenantRepository.GetByPublicIdAsync(
                command.TenantPublicId,
                cancellationToken);

        if (tenant is null)
        {
            return Result<StartProjectTaskResult>.Failure(
                TenantErrors.NotFound);
        }

        if (!tenant.IsActive)
        {
            return Result<StartProjectTaskResult>.Failure(
                TenantErrors.Inactive);
        }

        var startedByUser =
            await _userRepository.GetByPublicIdAsync(
                tenant.Id,
                command.StartedByUserPublicId,
                cancellationToken);

        if (startedByUser is null)
        {
            return Result<StartProjectTaskResult>.Failure(
                UserErrors.NotFound);
        }

        if (!startedByUser.IsActive)
        {
            return Result<StartProjectTaskResult>.Failure(
                UserErrors.Inactive);
        }

        await using var transaction =
            await _unitOfWork.BeginTransactionAsync(
                cancellationToken);

        try
        {
            var project =
                await _projectRepository
                    .GetForUpdateByPublicIdAsync(
                        tenant.Id,
                        command.ProjectPublicId,
                        cancellationToken);

            if (project is null)
            {
                await transaction.RollbackAsync(
                    cancellationToken);

                return Result<StartProjectTaskResult>.Failure(
                    ProjectErrors.NotFound);
            }

            if (project.Status != ProjectStatus.InProgress)
            {
                await transaction.RollbackAsync(
                    cancellationToken);

                return Result<StartProjectTaskResult>.Failure(
                    ProjectTaskErrors.StartBlockedByProjectStatus);
            }

            var projectTask =
                await _projectTaskRepository
                    .GetForUpdateByPublicIdAsync(
                        project.Id,
                        command.TaskPublicId,
                        cancellationToken);

            if (projectTask is null)
            {
                await transaction.RollbackAsync(
                    cancellationToken);

                return Result<StartProjectTaskResult>.Failure(
                    ProjectTaskErrors.NotFound);
            }

            if (projectTask.IsArchived)
            {
                await transaction.RollbackAsync(
                    cancellationToken);

                return Result<StartProjectTaskResult>.Failure(
                    ProjectTaskErrors.Archived);
            }

            if (projectTask.Status != ProjectTaskStatus.Todo)
            {
                await transaction.RollbackAsync(
                    cancellationToken);

                return Result<StartProjectTaskResult>.Failure(
                    ProjectTaskErrors.StartBlockedByTaskStatus);
            }

            if (projectTask.ResponsibleUserId is null)
            {
                await transaction.RollbackAsync(
                    cancellationToken);

                return Result<StartProjectTaskResult>.Failure(
                    ProjectTaskErrors.StartRequiresResponsible);
            }

            if (projectTask.ResponsibleUserId != startedByUser.Id)
            {
                await transaction.RollbackAsync(
                    cancellationToken);

                return Result<StartProjectTaskResult>.Failure(
                    ProjectTaskErrors.StartNotAllowed);
            }

            var projectMember =
                await _projectMemberRepository.GetActiveAsync(
                    project.Id,
                    startedByUser.Id,
                    cancellationToken);

            if (projectMember is null)
            {
                await transaction.RollbackAsync(
                    cancellationToken);

                return Result<StartProjectTaskResult>.Failure(
                    ProjectTaskErrors.StartNotAllowed);
            }

            projectTask.Start();

            await _unitOfWork.SaveChangesAsync(
                cancellationToken);

            await transaction.CommitAsync(
                cancellationToken);

            var response =
                new StartProjectTaskResult(
                    projectTask.PublicId,
                    tenant.PublicId,
                    project.PublicId,
                    startedByUser.PublicId,
                    projectTask.Status,
                    projectTask.UpdatedAt);

            return Result<StartProjectTaskResult>.Success(
                response);
        }
        catch
        {
            await transaction.RollbackAsync(
                cancellationToken);

            throw;
        }
    }

    private static void ValidatePublicIds(
        StartProjectTaskCommand command)
    {
        if (command.TenantPublicId == Guid.Empty)
        {
            throw new ArgumentException(
                "O identificador público da empresa não pode estar vazio.",
                nameof(command.TenantPublicId));
        }

        if (command.ProjectPublicId == Guid.Empty)
        {
            throw new ArgumentException(
                "O identificador público do projeto não pode estar vazio.",
                nameof(command.ProjectPublicId));
        }

        if (command.TaskPublicId == Guid.Empty)
        {
            throw new ArgumentException(
                "O identificador público da tarefa não pode estar vazio.",
                nameof(command.TaskPublicId));
        }

        if (command.StartedByUserPublicId == Guid.Empty)
        {
            throw new ArgumentException(
                "O identificador público do usuário não pode estar vazio.",
                nameof(command.StartedByUserPublicId));
        }
    }
}