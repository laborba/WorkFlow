using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Security.Claims;
using WorkFlow.API.Authorization;
using WorkFlow.API.Contracts.Common;
using WorkFlow.API.Contracts.ProjectTasks;
using WorkFlow.Application.Projects;
using WorkFlow.Application.ProjectTasks;
using WorkFlow.Application.ProjectTasks.AssignProjectTaskResponsible;
using WorkFlow.Application.ProjectTasks.ClaimProjectTask;
using WorkFlow.Application.ProjectTasks.CreateProjectTask;
using WorkFlow.Application.ProjectTasks.RemoveProjectTaskResponsible;
using WorkFlow.Application.ProjectTasks.UpdateProjectTask;
using WorkFlow.Application.ProjectTasks.ListProjectTasks;
using WorkFlow.Application.ProjectTasks.StartProjectTask;
using WorkFlow.Application.ProjectTasks.MoveProjectTaskToTodo;
using WorkFlow.Application.ProjectTasks.PauseProjectTask;
using WorkFlow.Application.ProjectTasks.ResumeProjectTask;
using WorkFlow.Application.ProjectTasks.SendProjectTaskToValidation;
using WorkFlow.Application.Tenants;
using WorkFlow.Application.Users;

namespace WorkFlow.API.Controllers;

[ApiController]
[Route(
    "api/tenants/{tenantPublicId:guid}/projects/{projectPublicId:guid}/tasks")]
public sealed class ProjectTasksController : ControllerBase
{
    private readonly CreateProjectTaskHandler
        _createProjectTaskHandler;

    private readonly AssignProjectTaskResponsibleHandler
        _assignProjectTaskResponsibleHandler;

    private readonly ClaimProjectTaskHandler
        _claimProjectTaskHandler;

    private readonly RemoveProjectTaskResponsibleHandler
        _removeProjectTaskResponsibleHandler;

    private readonly UpdateProjectTaskHandler
        _updateProjectTaskHandler;

    private readonly ListProjectTasksHandler
        _listProjectTasksHandler;

    private readonly StartProjectTaskHandler
        _startProjectTaskHandler;

    private readonly MoveProjectTaskToTodoHandler
        _moveProjectTaskToTodoHandler;

    private readonly PauseProjectTaskHandler
        _pauseProjectTaskHandler;

    private readonly ResumeProjectTaskHandler
        _resumeProjectTaskHandler;

    private readonly SendProjectTaskToValidationHandler
        _sendProjectTaskToValidationHandler;

    public ProjectTasksController(
        CreateProjectTaskHandler createProjectTaskHandler,
        AssignProjectTaskResponsibleHandler assignProjectTaskResponsibleHandler,
        ClaimProjectTaskHandler claimProjectTaskHandler,
        RemoveProjectTaskResponsibleHandler removeProjectTaskResponsibleHandler,
        UpdateProjectTaskHandler updateProjectTaskHandler,
        ListProjectTasksHandler listProjectTasksHandler,
        StartProjectTaskHandler startProjectTaskHandler,
        MoveProjectTaskToTodoHandler moveProjectTaskToTodoHandler,
        PauseProjectTaskHandler pauseProjectTaskHandler,
        ResumeProjectTaskHandler resumeProjectTaskHandler,
        SendProjectTaskToValidationHandler sendProjectTaskToValidationHandler)
    {
        _createProjectTaskHandler =
            createProjectTaskHandler;

        _assignProjectTaskResponsibleHandler =
            assignProjectTaskResponsibleHandler;

        _claimProjectTaskHandler =
            claimProjectTaskHandler;

        _removeProjectTaskResponsibleHandler =
            removeProjectTaskResponsibleHandler;

        _updateProjectTaskHandler =
            updateProjectTaskHandler;

        _listProjectTasksHandler =
            listProjectTasksHandler;

        _startProjectTaskHandler =
            startProjectTaskHandler;

        _moveProjectTaskToTodoHandler =
            moveProjectTaskToTodoHandler;

        _pauseProjectTaskHandler =
            pauseProjectTaskHandler;

        _resumeProjectTaskHandler =
            resumeProjectTaskHandler;

        _sendProjectTaskToValidationHandler =
            sendProjectTaskToValidationHandler;
    }


    [Authorize(
        Policy = AuthorizationPolicyNames.TenantAccess)]
    [HttpGet]
    [ProducesResponseType<ListProjectTasksResponse>(
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ErrorResponse>(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErrorResponse>(
        StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ErrorResponse>(
        StatusCodes.Status404NotFound)]
    [ProducesResponseType<ErrorResponse>(
        StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ListProjectTasksResponse>> List(
        Guid tenantPublicId,
        Guid projectPublicId,
        [FromQuery] ListProjectTasksRequest request,
        CancellationToken cancellationToken)
    {
        var userPublicIdValue =
            User.FindFirst(
                ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(
                userPublicIdValue,
                out var requestedByUserPublicId) ||
            requestedByUserPublicId == Guid.Empty)
        {
            return Unauthorized();
        }

        var query =
            new ListProjectTasksQuery(
                tenantPublicId,
                projectPublicId,
                requestedByUserPublicId,
                request.PageNumber,
                request.PageSize,
                request.Search,
                request.Status,
                request.Priority,
                request.ResponsibleUserPublicId,
                request.IsArchived);

        var result =
            await _listProjectTasksHandler.HandleAsync(
                query,
                cancellationToken);

        if (result.IsFailure)
        {
            var error =
                result.Error!;

            var errorResponse =
                new ErrorResponse(
                    error.Code,
                    error.Message);

            if (error == TenantErrors.NotFound ||
                error == UserErrors.NotFound ||
                error == ProjectErrors.NotFound)
            {
                return NotFound(
                    errorResponse);
            }

            if (error == TenantErrors.Inactive)
            {
                return Conflict(
                    errorResponse);
            }

            if (error == UserErrors.Inactive ||
                error == ProjectErrors.ViewNotAllowed)
            {
                return StatusCode(
                    StatusCodes.Status403Forbidden,
                    errorResponse);
            }

            return BadRequest(
                errorResponse);
        }

        var data =
            result.Value!;

        var items =
            data.Items
                .Select(task =>
                    new ProjectTaskListItemResponse(
                        task.PublicId,
                        task.CreatedByUserPublicId,
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
                        task.ArchivedAt))
                .ToArray();

        var response =
            new ListProjectTasksResponse(
                items,
                data.PageNumber,
                data.PageSize,
                data.TotalCount,
                data.TotalPages);

        return Ok(
            response);
    }

    [Authorize(
    Policy = AuthorizationPolicyNames.TenantAccess)]
    [HttpPut("{taskPublicId:guid}")]
    [ProducesResponseType<UpdateProjectTaskResponse>(
    StatusCodes.Status200OK)]
    [ProducesResponseType(
    StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ErrorResponse>(
    StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErrorResponse>(
    StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ErrorResponse>(
    StatusCodes.Status404NotFound)]
    [ProducesResponseType<ErrorResponse>(
    StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UpdateProjectTaskResponse>> Update(
    Guid tenantPublicId,
    Guid projectPublicId,
    Guid taskPublicId,
    [FromBody] UpdateProjectTaskRequest request,
    CancellationToken cancellationToken)
    {
        var userPublicIdValue =
            User.FindFirst(
                ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(
                userPublicIdValue,
                out var requestedByUserPublicId) ||
            requestedByUserPublicId == Guid.Empty)
        {
            return Unauthorized();
        }

        var command =
            new UpdateProjectTaskCommand(
                tenantPublicId,
                projectPublicId,
                taskPublicId,
                requestedByUserPublicId,
                request.Title,
                request.Description,
                request.Priority,
                request.DueDate);

        var result =
            await _updateProjectTaskHandler.HandleAsync(
                command,
                cancellationToken);

        if (result.IsFailure)
        {
            var error =
                result.Error!;

            var errorResponse =
                new ErrorResponse(
                    error.Code,
                    error.Message);

            if (error == TenantErrors.NotFound ||
                error == UserErrors.NotFound ||
                error == ProjectErrors.NotFound ||
                error == ProjectTaskErrors.NotFound)
            {
                return NotFound(
                    errorResponse);
            }

            if (error == UserErrors.Inactive ||
                error == ProjectTaskErrors.UpdateNotAllowed)
            {
                return StatusCode(
                    StatusCodes.Status403Forbidden,
                    errorResponse);
            }

            if (error == TenantErrors.Inactive ||
                error == ProjectTaskErrors
                    .UpdateBlockedByProjectStatus ||
                error == ProjectTaskErrors.Archived)
            {
                return Conflict(
                    errorResponse);
            }

            return BadRequest(
                errorResponse);
        }

        var task =
            result.Value!;

        var response =
            new UpdateProjectTaskResponse(
                task.PublicId,
                task.TenantPublicId,
                task.ProjectPublicId,
                task.Title,
                task.Description,
                task.Status,
                task.Priority,
                task.DueDate,
                task.UpdatedAt);

        return Ok(
            response);
    }

    [Authorize(
        Policy = AuthorizationPolicyNames.TenantAccess)]
    [HttpPost]
    [ProducesResponseType<CreateProjectTaskResponse>(
        StatusCodes.Status201Created)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ErrorResponse>(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErrorResponse>(
        StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ErrorResponse>(
        StatusCodes.Status404NotFound)]
    [ProducesResponseType<ErrorResponse>(
        StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CreateProjectTaskResponse>> Create(
        Guid tenantPublicId,
        Guid projectPublicId,
        [FromBody] CreateProjectTaskRequest request,
        CancellationToken cancellationToken)
    {
        var userPublicIdValue =
            User.FindFirst(
                ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(
                userPublicIdValue,
                out var createdByUserPublicId) ||
            createdByUserPublicId == Guid.Empty)
        {
            return Unauthorized();
        }

        var command =
            new CreateProjectTaskCommand(
                tenantPublicId,
                projectPublicId,
                createdByUserPublicId,
                request.Title,
                request.Description,
                request.Priority,
                request.DueDate);

        var result =
            await _createProjectTaskHandler.HandleAsync(
                command,
                cancellationToken);

        if (result.IsFailure)
        {
            var error =
                result.Error!;

            var errorResponse =
                new ErrorResponse(
                    error.Code,
                    error.Message);

            if (error == TenantErrors.NotFound ||
                error == UserErrors.NotFound ||
                error == ProjectErrors.NotFound)
            {
                return NotFound(
                    errorResponse);
            }

            if (error == TenantErrors.Inactive ||
                error == ProjectTaskErrors
                    .CreationBlockedByProjectStatus)
            {
                return Conflict(
                    errorResponse);
            }

            if (error == UserErrors.Inactive ||
                error == ProjectTaskErrors.CreationNotAllowed)
            {
                return StatusCode(
                    StatusCodes.Status403Forbidden,
                    errorResponse);
            }

            return BadRequest(
                errorResponse);
        }

        var task =
            result.Value!;

        var response =
            new CreateProjectTaskResponse(
                task.PublicId,
                task.TenantPublicId,
                task.ProjectPublicId,
                task.CreatedByUserPublicId,
                task.ResponsibleUserPublicId,
                task.Title,
                task.Description,
                task.Status,
                task.Priority,
                task.DueDate,
                task.CreatedAt);

        return StatusCode(
            StatusCodes.Status201Created,
            response);
    }

    [Authorize(
        Policy = AuthorizationPolicyNames.TenantAccess)]
    [HttpPatch(
        "{taskPublicId:guid}/responsible")]
    [ProducesResponseType<AssignProjectTaskResponsibleResponse>(
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ErrorResponse>(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErrorResponse>(
        StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ErrorResponse>(
        StatusCodes.Status404NotFound)]
    [ProducesResponseType<ErrorResponse>(
        StatusCodes.Status409Conflict)]
    public async Task<
        ActionResult<AssignProjectTaskResponsibleResponse>>
        AssignResponsible(
            Guid tenantPublicId,
            Guid projectPublicId,
            Guid taskPublicId,
            [FromBody] AssignProjectTaskResponsibleRequest request,
            CancellationToken cancellationToken)
    {
        var userPublicIdValue =
            User.FindFirst(
                ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(
                userPublicIdValue,
                out var requestedByUserPublicId) ||
            requestedByUserPublicId == Guid.Empty)
        {
            return Unauthorized();
        }

        var command =
            new AssignProjectTaskResponsibleCommand(
                tenantPublicId,
                projectPublicId,
                taskPublicId,
                requestedByUserPublicId,
                request.ResponsibleUserPublicId);

        var result =
            await _assignProjectTaskResponsibleHandler
                .HandleAsync(
                    command,
                    cancellationToken);

        if (result.IsFailure)
        {
            var error =
                result.Error!;

            var errorResponse =
                new ErrorResponse(
                    error.Code,
                    error.Message);

            if (error == TenantErrors.NotFound ||
                error == UserErrors.NotFound ||
                error == ProjectErrors.NotFound ||
                error == ProjectTaskErrors.NotFound ||
                error == ProjectTaskErrors
                    .ResponsibleUserNotFound)
            {
                return NotFound(
                    errorResponse);
            }

            if (error == TenantErrors.Inactive ||
                error == ProjectTaskErrors
                    .AssignmentBlockedByProjectStatus ||
                error == ProjectTaskErrors.Archived ||
                error == ProjectTaskErrors
                    .ResponsibleUserInactive ||
                error == ProjectTaskErrors
                    .ResponsibleUserNotActiveMember)
            {
                return Conflict(
                    errorResponse);
            }

            if (error == UserErrors.Inactive ||
                error == ProjectTaskErrors
                    .AssignmentNotAllowed)
            {
                return StatusCode(
                    StatusCodes.Status403Forbidden,
                    errorResponse);
            }

            return BadRequest(
                errorResponse);
        }

        var task =
            result.Value!;

        var response =
            new AssignProjectTaskResponsibleResponse(
                task.PublicId,
                task.TenantPublicId,
                task.ProjectPublicId,
                task.ResponsibleUserPublicId,
                task.Status,
                task.UpdatedAt);

        return Ok(
            response);
    }

    [Authorize(
        Policy = AuthorizationPolicyNames.TenantAccess)]
    [HttpPatch("{taskPublicId:guid}/claim")]
    [ProducesResponseType<ClaimProjectTaskResponse>(
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ErrorResponse>(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErrorResponse>(
        StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ErrorResponse>(
        StatusCodes.Status404NotFound)]
    [ProducesResponseType<ErrorResponse>(
        StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ClaimProjectTaskResponse>> Claim(
        Guid tenantPublicId,
        Guid projectPublicId,
        Guid taskPublicId,
        CancellationToken cancellationToken)
    {
        var userPublicIdValue =
            User.FindFirst(
                ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(
                userPublicIdValue,
                out var claimedByUserPublicId) ||
            claimedByUserPublicId == Guid.Empty)
        {
            return Unauthorized();
        }

        var command =
            new ClaimProjectTaskCommand(
                tenantPublicId,
                projectPublicId,
                taskPublicId,
                claimedByUserPublicId);

        var result =
            await _claimProjectTaskHandler.HandleAsync(
                command,
                cancellationToken);

        if (result.IsFailure)
        {
            var error =
                result.Error!;

            var errorResponse =
                new ErrorResponse(
                    error.Code,
                    error.Message);

            if (error == TenantErrors.NotFound ||
                error == UserErrors.NotFound ||
                error == ProjectErrors.NotFound ||
                error == ProjectTaskErrors.NotFound)
            {
                return NotFound(
                    errorResponse);
            }

            if (error == TenantErrors.Inactive ||
                error == ProjectTaskErrors.ClaimBlockedByProjectStatus ||
                error == ProjectTaskErrors.ClaimBlockedByTaskStatus ||
                error == ProjectTaskErrors.AlreadyAssigned ||
                error == ProjectTaskErrors.Archived)
            {
                return Conflict(
                    errorResponse);
            }

            if (error == UserErrors.Inactive ||
                error == ProjectTaskErrors.ClaimNotAllowed)
            {
                return StatusCode(
                    StatusCodes.Status403Forbidden,
                    errorResponse);
            }

            return BadRequest(
                errorResponse);
        }

        var task =
            result.Value!;

        var response =
            new ClaimProjectTaskResponse(
                task.PublicId,
                task.TenantPublicId,
                task.ProjectPublicId,
                task.ResponsibleUserPublicId,
                task.Status,
                task.UpdatedAt);

        return Ok(
            response);
    }

    [Authorize(
        Policy = AuthorizationPolicyNames.TenantAccess)]
    [HttpDelete("{taskPublicId:guid}/responsible")]
    [ProducesResponseType<RemoveProjectTaskResponsibleResponse>(
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ErrorResponse>(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErrorResponse>(
        StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ErrorResponse>(
        StatusCodes.Status404NotFound)]
    [ProducesResponseType<ErrorResponse>(
        StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RemoveProjectTaskResponsibleResponse>>
    RemoveResponsible(
        Guid tenantPublicId,
        Guid projectPublicId,
        Guid taskPublicId,
        CancellationToken cancellationToken)
    {
        var userPublicIdValue =
            User.FindFirst(
                ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(
                userPublicIdValue,
                out var requestedByUserPublicId) ||
            requestedByUserPublicId == Guid.Empty)
        {
            return Unauthorized();
        }

        var command =
            new RemoveProjectTaskResponsibleCommand(
                tenantPublicId,
                projectPublicId,
                taskPublicId,
                requestedByUserPublicId);

        var result =
            await _removeProjectTaskResponsibleHandler
                .HandleAsync(
                    command,
                    cancellationToken);

        if (result.IsFailure)
        {
            var error =
                result.Error!;

            var errorResponse =
                new ErrorResponse(
                    error.Code,
                    error.Message);

            if (error == TenantErrors.NotFound ||
                error == UserErrors.NotFound ||
                error == ProjectErrors.NotFound ||
                error == ProjectTaskErrors.NotFound)
            {
                return NotFound(
                    errorResponse);
            }

            if (error == UserErrors.Inactive ||
                error ==
                ProjectTaskErrors.ResponsibleRemovalNotAllowed)
            {
                return StatusCode(
                    StatusCodes.Status403Forbidden,
                    errorResponse);
            }

            if (error == TenantErrors.Inactive ||
                error ==
                ProjectTaskErrors
                    .ResponsibleRemovalBlockedByProjectStatus ||
                error ==
                ProjectTaskErrors
                    .ResponsibleRemovalBlockedByTaskStatus ||
                error == ProjectTaskErrors.Archived)
            {
                return Conflict(
                    errorResponse);
            }

            return BadRequest(
                errorResponse);
        }

        var task =
            result.Value!;

        var response =
            new RemoveProjectTaskResponsibleResponse(
                task.PublicId,
                task.TenantPublicId,
                task.ProjectPublicId,
                task.ResponsibleUserPublicId,
                task.Status,
                task.UpdatedAt);

        return Ok(
            response);
    }

    [Authorize(
    Policy = AuthorizationPolicyNames.TenantAccess)]
    [HttpPost("{taskPublicId:guid}/start")]
    [ProducesResponseType<StartProjectTaskResponse>(
    StatusCodes.Status200OK)]
    [ProducesResponseType(
    StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ErrorResponse>(
    StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErrorResponse>(
    StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ErrorResponse>(
    StatusCodes.Status404NotFound)]
    [ProducesResponseType<ErrorResponse>(
    StatusCodes.Status409Conflict)]
    public async Task<ActionResult<StartProjectTaskResponse>> Start(
    Guid tenantPublicId,
    Guid projectPublicId,
    Guid taskPublicId,
    CancellationToken cancellationToken)
    {
        var userPublicIdValue =
            User.FindFirst(
                ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(
                userPublicIdValue,
                out var startedByUserPublicId) ||
            startedByUserPublicId == Guid.Empty)
        {
            return Unauthorized();
        }

        var command =
            new StartProjectTaskCommand(
                tenantPublicId,
                projectPublicId,
                taskPublicId,
                startedByUserPublicId);

        var result =
            await _startProjectTaskHandler.HandleAsync(
                command,
                cancellationToken);

        if (result.IsFailure)
        {
            var error =
                result.Error!;

            var errorResponse =
                new ErrorResponse(
                    error.Code,
                    error.Message);

            if (error == TenantErrors.NotFound ||
                error == UserErrors.NotFound ||
                error == ProjectErrors.NotFound ||
                error == ProjectTaskErrors.NotFound)
            {
                return NotFound(
                    errorResponse);
            }

            if (error == UserErrors.Inactive ||
                error == ProjectTaskErrors.StartNotAllowed)
            {
                return StatusCode(
                    StatusCodes.Status403Forbidden,
                    errorResponse);
            }

            if (error == TenantErrors.Inactive ||
                error == ProjectTaskErrors.StartBlockedByProjectStatus ||
                error == ProjectTaskErrors.StartBlockedByTaskStatus ||
                error == ProjectTaskErrors.StartRequiresResponsible ||
                error == ProjectTaskErrors.Archived)
            {
                return Conflict(
                    errorResponse);
            }

            return BadRequest(
                errorResponse);
        }

        var task =
            result.Value!;

        var response =
            new StartProjectTaskResponse(
                task.PublicId,
                task.TenantPublicId,
                task.ProjectPublicId,
                task.ResponsibleUserPublicId,
                task.Status,
                task.UpdatedAt);

        return Ok(
            response);
    }

    [Authorize(
        Policy = AuthorizationPolicyNames.TenantAccess)]
    [HttpPost("{taskPublicId:guid}/pause")]
    [ProducesResponseType<PauseProjectTaskResponse>(
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ErrorResponse>(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErrorResponse>(
        StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ErrorResponse>(
        StatusCodes.Status404NotFound)]
    [ProducesResponseType<ErrorResponse>(
        StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PauseProjectTaskResponse>> Pause(
        Guid tenantPublicId,
        Guid projectPublicId,
        Guid taskPublicId,
        [FromBody] PauseProjectTaskRequest request,
        CancellationToken cancellationToken)
    {
        var userPublicIdValue =
            User.FindFirst(
                ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(
                userPublicIdValue,
                out var requestedByUserPublicId) ||
            requestedByUserPublicId == Guid.Empty)
        {
            return Unauthorized();
        }

        var command =
            new PauseProjectTaskCommand(
                tenantPublicId,
                projectPublicId,
                taskPublicId,
                requestedByUserPublicId,
                request.Reason);

        var result =
            await _pauseProjectTaskHandler.HandleAsync(
                command,
                cancellationToken);

        if (result.IsFailure)
        {
            var error =
                result.Error!;

            var errorResponse =
                new ErrorResponse(
                    error.Code,
                    error.Message);

            if (error == TenantErrors.NotFound ||
                error == UserErrors.NotFound ||
                error == ProjectErrors.NotFound ||
                error == ProjectTaskErrors.NotFound)
            {
                return NotFound(
                    errorResponse);
            }

            if (error == UserErrors.Inactive ||
                error == ProjectTaskErrors.PauseNotAllowed)
            {
                return StatusCode(
                    StatusCodes.Status403Forbidden,
                    errorResponse);
            }

            if (error == TenantErrors.Inactive ||
                error ==
                ProjectTaskErrors.PauseBlockedByProjectStatus ||
                error ==
                ProjectTaskErrors.PauseBlockedByTaskStatus ||
                error ==
                ProjectTaskErrors.PauseRequiresResponsible ||
                error == ProjectTaskErrors.Archived)
            {
                return Conflict(
                    errorResponse);
            }

            return BadRequest(
                errorResponse);
        }

        var task =
            result.Value!;

        var response =
            new PauseProjectTaskResponse(
                task.PublicId,
                task.TenantPublicId,
                task.ProjectPublicId,
                task.ResponsibleUserPublicId,
                task.Status,
                task.StatusBeforePause,
                task.DueDate,
                task.UpdatedAt);

        return Ok(
            response);
    }

    [Authorize(
        Policy = AuthorizationPolicyNames.TenantAccess)]
    [HttpPost("{taskPublicId:guid}/resume")]
    [ProducesResponseType<ResumeProjectTaskResponse>(
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ErrorResponse>(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErrorResponse>(
        StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ErrorResponse>(
        StatusCodes.Status404NotFound)]
    [ProducesResponseType<ErrorResponse>(
        StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ResumeProjectTaskResponse>> Resume(
        Guid tenantPublicId,
        Guid projectPublicId,
        Guid taskPublicId,
    [FromBody(
        EmptyBodyBehavior =
            EmptyBodyBehavior.Allow)]
    ResumeProjectTaskRequest? request,
    CancellationToken cancellationToken)
    {
        var userPublicIdValue =
            User.FindFirst(
                ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(
                userPublicIdValue,
                out var requestedByUserPublicId) ||
            requestedByUserPublicId == Guid.Empty)
        {
            return Unauthorized();
        }

        var command =
            new ResumeProjectTaskCommand(
                tenantPublicId,
                projectPublicId,
                taskPublicId,
                requestedByUserPublicId,
                request?.NewDueDate);

        var result =
            await _resumeProjectTaskHandler.HandleAsync(
                command,
                cancellationToken);

        if (result.IsFailure)
        {
            var error =
                result.Error!;

            var errorResponse =
                new ErrorResponse(
                    error.Code,
                    error.Message);

            if (error == TenantErrors.NotFound ||
                error == UserErrors.NotFound ||
                error == ProjectErrors.NotFound ||
                error == ProjectTaskErrors.NotFound)
            {
                return NotFound(
                    errorResponse);
            }

            if (error == UserErrors.Inactive ||
                error == ProjectTaskErrors.ResumeNotAllowed)
            {
                return StatusCode(
                    StatusCodes.Status403Forbidden,
                    errorResponse);
            }

            if (error == TenantErrors.Inactive ||
                error ==
                ProjectTaskErrors.ResumeBlockedByProjectStatus ||
                error ==
                ProjectTaskErrors.ResumeBlockedByTaskStatus ||
                error ==
                ProjectTaskErrors.ResumeRequiresResponsible ||
                error == ProjectTaskErrors.Archived)
            {
                return Conflict(
                    errorResponse);
            }

            return BadRequest(
                errorResponse);
        }

        var task =
            result.Value!;

        var response =
            new ResumeProjectTaskResponse(
                task.PublicId,
                task.TenantPublicId,
                task.ProjectPublicId,
                task.ResponsibleUserPublicId,
                task.Status,
                task.StatusBeforePause,
                task.DueDate,
                task.UpdatedAt);

        return Ok(
    response);
    }

    [Authorize(
        Policy = AuthorizationPolicyNames.TenantAccess)]
    [HttpPost("{taskPublicId:guid}/validation")]
    [ProducesResponseType<SendProjectTaskToValidationResponse>(
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ErrorResponse>(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErrorResponse>(
        StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ErrorResponse>(
        StatusCodes.Status404NotFound)]
    [ProducesResponseType<ErrorResponse>(
        StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SendProjectTaskToValidationResponse>>
        SendToValidation(
            Guid tenantPublicId,
            Guid projectPublicId,
            Guid taskPublicId,
            CancellationToken cancellationToken)
    {
        var userPublicIdValue =
            User.FindFirst(
                ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(
                userPublicIdValue,
                out var sentByUserPublicId) ||
            sentByUserPublicId == Guid.Empty)
        {
            return Unauthorized();
        }

        var command =
            new SendProjectTaskToValidationCommand(
                tenantPublicId,
                projectPublicId,
                taskPublicId,
                sentByUserPublicId);

        var result =
            await _sendProjectTaskToValidationHandler
                .HandleAsync(
                    command,
                    cancellationToken);

        if (result.IsFailure)
        {
            var error =
                result.Error!;

            var errorResponse =
                new ErrorResponse(
                    error.Code,
                    error.Message);

            if (error == TenantErrors.NotFound ||
                error == UserErrors.NotFound ||
                error == ProjectErrors.NotFound ||
                error == ProjectTaskErrors.NotFound)
            {
                return NotFound(
                    errorResponse);
            }

            if (error == UserErrors.Inactive ||
                error ==
                ProjectTaskErrors.SendToValidationNotAllowed)
            {
                return StatusCode(
                    StatusCodes.Status403Forbidden,
                    errorResponse);
            }

            if (error == TenantErrors.Inactive ||
                error ==
                ProjectTaskErrors
                    .SendToValidationBlockedByProjectStatus ||
                error ==
                ProjectTaskErrors
                    .SendToValidationBlockedByTaskStatus ||
                error ==
                ProjectTaskErrors
                    .SendToValidationRequiresResponsible ||
                error == ProjectTaskErrors.Archived)
            {
                return Conflict(
                    errorResponse);
            }

            return BadRequest(
                errorResponse);
        }

        var task =
            result.Value!;

        var response =
            new SendProjectTaskToValidationResponse(
                task.PublicId,
                task.TenantPublicId,
                task.ProjectPublicId,
                task.ResponsibleUserPublicId,
                task.ValidatorUserPublicId,
                task.Status,
                task.UpdatedAt);

        return Ok(
            response);
    }

    [Authorize(
        Policy = AuthorizationPolicyNames.TenantAccess)]
    [HttpPost("{taskPublicId:guid}/todo")]
    [ProducesResponseType<MoveProjectTaskToTodoResponse>(
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ErrorResponse>(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErrorResponse>(
        StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ErrorResponse>(
        StatusCodes.Status404NotFound)]
    [ProducesResponseType<ErrorResponse>(
        StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MoveProjectTaskToTodoResponse>>
    MoveToTodo(
        Guid tenantPublicId,
        Guid projectPublicId,
        Guid taskPublicId,
        CancellationToken cancellationToken)
    {
        var userPublicIdValue =
            User.FindFirst(
                ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(
                userPublicIdValue,
                out var requestedByUserPublicId) ||
            requestedByUserPublicId == Guid.Empty)
        {
            return Unauthorized();
        }

        var command =
            new MoveProjectTaskToTodoCommand(
                tenantPublicId,
                projectPublicId,
                taskPublicId,
                requestedByUserPublicId);

        var result =
            await _moveProjectTaskToTodoHandler.HandleAsync(
                command,
                cancellationToken);

        if (result.IsFailure)
        {
            var error =
                result.Error!;

            var errorResponse =
                new ErrorResponse(
                    error.Code,
                    error.Message);

            if (error == TenantErrors.NotFound ||
                error == UserErrors.NotFound ||
                error == ProjectErrors.NotFound ||
                error == ProjectTaskErrors.NotFound)
            {
                return NotFound(
                    errorResponse);
            }

            if (error == UserErrors.Inactive ||
                error ==
                ProjectTaskErrors.MoveToTodoNotAllowed)
            {
                return StatusCode(
                    StatusCodes.Status403Forbidden,
                    errorResponse);
            }

            if (error == TenantErrors.Inactive ||
                error ==
                ProjectTaskErrors
                    .MoveToTodoBlockedByProjectStatus ||
                error ==
                ProjectTaskErrors
                    .MoveToTodoBlockedByTaskStatus ||
                error == ProjectTaskErrors.Archived)
            {
                return Conflict(
                    errorResponse);
            }

            return BadRequest(
                errorResponse);
        }

        var task =
            result.Value!;

        var response =
            new MoveProjectTaskToTodoResponse(
                task.PublicId,
                task.TenantPublicId,
                task.ProjectPublicId,
                task.Status,
                task.UpdatedAt);

        return Ok(
            response);
    }
}