using WorkFlow.Application.Abstractions.Persistence;
using WorkFlow.Application.Common.Results;
using WorkFlow.Application.Projects;
using WorkFlow.Application.Tenants;
using WorkFlow.Application.Users;
using WorkFlow.Domain.Enums;

namespace WorkFlow.Application.ProjectTasks.ResumeProjectTask;

public sealed class ResumeProjectTaskHandler
{
    private readonly ITenantRepository _tenantRepository;
    private readonly IUserRepository _userRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IProjectMemberRepository _projectMemberRepository;
    private readonly IProjectTaskRepository _projectTaskRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ResumeProjectTaskHandler(
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

    public async Task<Result<ResumeProjectTaskResult>> HandleAsync(
        ResumeProjectTaskCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            command);

        ValidatePublicIds(
            command);

        var normalizedNewDueDate =
            NormalizeNewDueDate(
                command.NewDueDate);

        var tenant =
            await _tenantRepository.GetByPublicIdAsync(
                command.TenantPublicId,
                cancellationToken);

        if (tenant is null)
        {
            return Result<ResumeProjectTaskResult>.Failure(
                TenantErrors.NotFound);
        }

        if (!tenant.IsActive)
        {
            return Result<ResumeProjectTaskResult>.Failure(
                TenantErrors.Inactive);
        }

        var requestedByUser =
            await _userRepository.GetByPublicIdAsync(
                tenant.Id,
                command.RequestedByUserPublicId,
                cancellationToken);

        if (requestedByUser is null)
        {
            return Result<ResumeProjectTaskResult>.Failure(
                UserErrors.NotFound);
        }

        if (!requestedByUser.IsActive)
        {
            return Result<ResumeProjectTaskResult>.Failure(
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

                return Result<ResumeProjectTaskResult>.Failure(
                    ProjectErrors.NotFound);
            }

            if (project.Status != ProjectStatus.InProgress)
            {
                await transaction.RollbackAsync(
                    cancellationToken);

                return Result<ResumeProjectTaskResult>.Failure(
                    ProjectTaskErrors.ResumeBlockedByProjectStatus);
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

                return Result<ResumeProjectTaskResult>.Failure(
                    ProjectTaskErrors.NotFound);
            }

            if (projectTask.IsArchived)
            {
                await transaction.RollbackAsync(
                    cancellationToken);

                return Result<ResumeProjectTaskResult>.Failure(
                    ProjectTaskErrors.Archived);
            }

            if (projectTask.Status != ProjectTaskStatus.Paused)
            {
                await transaction.RollbackAsync(
                    cancellationToken);

                return Result<ResumeProjectTaskResult>.Failure(
                    ProjectTaskErrors.ResumeBlockedByTaskStatus);
            }

            if (!projectTask.ResponsibleUserId.HasValue)
            {
                await transaction.RollbackAsync(
                    cancellationToken);

                return Result<ResumeProjectTaskResult>.Failure(
                    ProjectTaskErrors.ResumeRequiresResponsible);
            }

            if (projectTask.ResponsibleUserId.Value !=
                requestedByUser.Id)
            {
                await transaction.RollbackAsync(
                    cancellationToken);

                return Result<ResumeProjectTaskResult>.Failure(
                    ProjectTaskErrors.ResumeNotAllowed);
            }

            var isActiveMember =
                await _projectMemberRepository.IsActiveMemberAsync(
                    project.Id,
                    requestedByUser.Id,
                    cancellationToken);

            if (!isActiveMember)
            {
                await transaction.RollbackAsync(
                    cancellationToken);

                return Result<ResumeProjectTaskResult>.Failure(
                    ProjectTaskErrors.ResumeNotAllowed);
            }

            projectTask.Resume();

            if (normalizedNewDueDate.HasValue)
            {
                projectTask.ChangeDueDate(
                    normalizedNewDueDate.Value);
            }

            await _unitOfWork.SaveChangesAsync(
                cancellationToken);

            await transaction.CommitAsync(
                cancellationToken);

            var result =
                new ResumeProjectTaskResult(
                    projectTask.PublicId,
                    tenant.PublicId,
                    project.PublicId,
                    requestedByUser.PublicId,
                    projectTask.Status,
                    projectTask.StatusBeforePause,
                    projectTask.DueDate,
                    projectTask.UpdatedAt);

            return Result<ResumeProjectTaskResult>.Success(
                result);
        }
        catch
        {
            await transaction.RollbackAsync(
                cancellationToken);

            throw;
        }
    }

    private static void ValidatePublicIds(
        ResumeProjectTaskCommand command)
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

        if (command.RequestedByUserPublicId == Guid.Empty)
        {
            throw new ArgumentException(
                "O identificador público do usuário solicitante não pode estar vazio.",
                nameof(command.RequestedByUserPublicId));
        }
    }

    private static DateTime? NormalizeNewDueDate(
        DateTime? newDueDate)
    {
        if (!newDueDate.HasValue)
            return null;

        return newDueDate.Value.Kind switch
        {
            DateTimeKind.Utc =>
                newDueDate.Value,

            DateTimeKind.Local =>
                newDueDate.Value.ToUniversalTime(),

            DateTimeKind.Unspecified =>
                throw new ArgumentException(
                    "O novo prazo da tarefa deve informar UTC ou um fuso horário.",
                    nameof(ResumeProjectTaskCommand.NewDueDate)),

            _ =>
                throw new ArgumentOutOfRangeException(
                    nameof(newDueDate))
        };
    }
}