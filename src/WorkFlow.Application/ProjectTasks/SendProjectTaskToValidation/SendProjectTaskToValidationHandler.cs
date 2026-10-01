using WorkFlow.Application.Abstractions.Persistence;
using WorkFlow.Application.Common.Results;
using WorkFlow.Application.Projects;
using WorkFlow.Application.Tenants;
using WorkFlow.Application.Users;
using WorkFlow.Domain.Enums;

namespace WorkFlow.Application.ProjectTasks.SendProjectTaskToValidation;

public sealed class SendProjectTaskToValidationHandler
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

    public SendProjectTaskToValidationHandler(
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

    public async Task<Result<SendProjectTaskToValidationResult>>
        HandleAsync(
            SendProjectTaskToValidationCommand command,
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
            return Result<SendProjectTaskToValidationResult>.Failure(
                TenantErrors.NotFound);
        }

        if (!tenant.IsActive)
        {
            return Result<SendProjectTaskToValidationResult>.Failure(
                TenantErrors.Inactive);
        }

        var sentByUser =
            await _userRepository.GetByPublicIdAsync(
                tenant.Id,
                command.SentByUserPublicId,
                cancellationToken);

        if (sentByUser is null)
        {
            return Result<SendProjectTaskToValidationResult>.Failure(
                UserErrors.NotFound);
        }

        if (!sentByUser.IsActive)
        {
            return Result<SendProjectTaskToValidationResult>.Failure(
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

                return Result<SendProjectTaskToValidationResult>.Failure(
                    ProjectErrors.NotFound);
            }

            if (project.Status != ProjectStatus.InProgress)
            {
                await transaction.RollbackAsync(
                    cancellationToken);

                return Result<SendProjectTaskToValidationResult>.Failure(
                    ProjectTaskErrors
                        .SendToValidationBlockedByProjectStatus);
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

                return Result<SendProjectTaskToValidationResult>.Failure(
                    ProjectTaskErrors.NotFound);
            }

            if (projectTask.IsArchived)
            {
                await transaction.RollbackAsync(
                    cancellationToken);

                return Result<SendProjectTaskToValidationResult>.Failure(
                    ProjectTaskErrors.Archived);
            }

            if (projectTask.Status != ProjectTaskStatus.InProgress)
            {
                await transaction.RollbackAsync(
                    cancellationToken);

                return Result<SendProjectTaskToValidationResult>.Failure(
                    ProjectTaskErrors
                        .SendToValidationBlockedByTaskStatus);
            }

            if (projectTask.ResponsibleUserId is null)
            {
                await transaction.RollbackAsync(
                    cancellationToken);

                return Result<SendProjectTaskToValidationResult>.Failure(
                    ProjectTaskErrors
                        .SendToValidationRequiresResponsible);
            }

            if (projectTask.ResponsibleUserId != sentByUser.Id)
            {
                await transaction.RollbackAsync(
                    cancellationToken);

                return Result<SendProjectTaskToValidationResult>.Failure(
                    ProjectTaskErrors.SendToValidationNotAllowed);
            }

            var projectMember =
                await _projectMemberRepository.GetActiveAsync(
                    project.Id,
                    sentByUser.Id,
                    cancellationToken);

            if (projectMember is null)
            {
                await transaction.RollbackAsync(
                    cancellationToken);

                return Result<SendProjectTaskToValidationResult>.Failure(
                    ProjectTaskErrors.SendToValidationNotAllowed);
            }

            projectTask.SendToValidation();

            await _unitOfWork.SaveChangesAsync(
                cancellationToken);

            await transaction.CommitAsync(
                cancellationToken);

            var response =
                new SendProjectTaskToValidationResult(
                    projectTask.PublicId,
                    tenant.PublicId,
                    project.PublicId,
                    sentByUser.PublicId,
                    null,
                    projectTask.Status,
                    projectTask.UpdatedAt);

            return Result<SendProjectTaskToValidationResult>.Success(
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
        SendProjectTaskToValidationCommand command)
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

        if (command.SentByUserPublicId == Guid.Empty)
        {
            throw new ArgumentException(
                "O identificador público do usuário não pode estar vazio.",
                nameof(command.SentByUserPublicId));
        }
    }
}