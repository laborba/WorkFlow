using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WorkFlow.API.Authorization;
using WorkFlow.API.Contracts.Common;
using WorkFlow.Application.Common.Errors;
using WorkFlow.API.Contracts.Projects;
using WorkFlow.Application.Projects;
using WorkFlow.Application.Projects.CreateProject;
using WorkFlow.Application.Projects.GetProjectByPublicId;
using WorkFlow.Application.Projects.ListProjects;
using WorkFlow.Application.Projects.PauseProject;
using WorkFlow.Application.Projects.ResumeProject;
using WorkFlow.Application.Projects.StartProject;
using WorkFlow.Application.Projects.UpdateProject;
using WorkFlow.Application.Tenants;
using WorkFlow.Application.Users;
using WorkFlow.Domain.Enums;

namespace WorkFlow.API.Controllers;

[ApiController]
[Route("api/tenants/{tenantPublicId:guid}/projects")]
public sealed class ProjectsController :
    ControllerBase
{
    private readonly CreateProjectHandler
        _createProjectHandler;

    private readonly GetProjectByPublicIdHandler
        _getProjectByPublicIdHandler;

    private readonly ListProjectsHandler
        _listProjectsHandler;

    private readonly UpdateProjectHandler
        _updateProjectHandler;

    private readonly StartProjectHandler
        _startProjectHandler;

    private readonly PauseProjectHandler
        _pauseProjectHandler;

    private readonly ResumeProjectHandler
        _resumeProjectHandler;

    public ProjectsController(
        CreateProjectHandler createProjectHandler,
        GetProjectByPublicIdHandler getProjectByPublicIdHandler,
        ListProjectsHandler listProjectsHandler,
        UpdateProjectHandler updateProjectHandler,
        StartProjectHandler startProjectHandler,
        PauseProjectHandler pauseProjectHandler,
        ResumeProjectHandler resumeProjectHandler)
    {
        _createProjectHandler =
            createProjectHandler;

        _getProjectByPublicIdHandler =
            getProjectByPublicIdHandler;

        _listProjectsHandler =
            listProjectsHandler;

        _updateProjectHandler =
            updateProjectHandler;

        _startProjectHandler =
            startProjectHandler;

        _pauseProjectHandler =
            pauseProjectHandler;

        _resumeProjectHandler =
            resumeProjectHandler;
    }

    [Authorize(
        Policy = AuthorizationPolicyNames.TenantAccess)]
    [Authorize(
        Policy = AuthorizationPolicyNames.ProjectCreation)]
    [HttpPost]
    [ProducesResponseType<CreateProjectResponse>(
        StatusCodes.Status201Created)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ErrorResponse>(
        StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ErrorResponse>(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErrorResponse>(
        StatusCodes.Status404NotFound)]
    [ProducesResponseType<ErrorResponse>(
        StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CreateProjectResponse>> Create(
        Guid tenantPublicId,
        [FromBody] CreateProjectRequest request,
        CancellationToken cancellationToken)
    {
        var userPublicIdValue =
            User.FindFirst(
                ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(
                userPublicIdValue,
                out var userPublicId))
        {
            return Unauthorized();
        }

        var command =
            new CreateProjectCommand(
                tenantPublicId,
                userPublicId,
                request.Name,
                request.Description,
                request.DueDate);

        var result =
            await _createProjectHandler.HandleAsync(
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
                error == UserErrors.NotFound)
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
                error == ProjectErrors.CreationNotAllowed)
            {
                return StatusCode(
                    StatusCodes.Status403Forbidden,
                    errorResponse);
            }

            return BadRequest(
                errorResponse);
        }

        var createdProject =
            result.Value!;

        var response =
            new CreateProjectResponse(
                createdProject.PublicId,
                createdProject.TenantPublicId,
                createdProject.CreatedByUserPublicId,
                createdProject.Name,
                createdProject.Description,
                createdProject.Status,
                createdProject.DueDate,
                createdProject.CreatedAt);

        return Created(
            $"/api/tenants/" +
            $"{response.TenantPublicId}/projects/" +
            $"{response.PublicId}",
            response);
    }

    [Authorize(
        Policy = AuthorizationPolicyNames.TenantAccess)]
    [HttpGet]
    [ProducesResponseType<ListProjectsResponse>(
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ErrorResponse>(
        StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ErrorResponse>(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErrorResponse>(
        StatusCodes.Status404NotFound)]
    [ProducesResponseType<ErrorResponse>(
        StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ListProjectsResponse>> List(
        Guid tenantPublicId,
        [FromQuery] ListProjectsRequest request,
        CancellationToken cancellationToken)
    {
        var userPublicIdValue =
            User.FindFirst(
                ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(
                userPublicIdValue,
                out var userPublicId))
        {
            return Unauthorized();
        }

        ProjectStatus? status =
            request.Status.HasValue
                ? (ProjectStatus)request.Status.Value
                : null;

        var query =
            new ListProjectsQuery(
                tenantPublicId,
                userPublicId,
                request.PageNumber,
                request.PageSize,
                request.Search,
                status,
                request.ResponsibleUserPublicId);

        var result =
            await _listProjectsHandler.HandleAsync(
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
                error == UserErrors.NotFound)
            {
                return NotFound(
                    errorResponse);
            }

            if (error == TenantErrors.Inactive)
            {
                return Conflict(
                    errorResponse);
            }

            if (error == UserErrors.Inactive)
            {
                return StatusCode(
                    StatusCodes.Status403Forbidden,
                    errorResponse);
            }

            return BadRequest(
                errorResponse);
        }

        var projects =
            result.Value!;

        var items =
            projects.Items
                .Select(project =>
                    new ProjectListItemResponse(
                        project.PublicId,
                        project.Name,
                        project.Description,
                        project.Status,
                        project.ResponsibleUserPublicId,
                        project.ResponsibleUserName,
                        project.DueDate,
                        project.CreatedAt,
                        project.UpdatedAt,
                        project.ArchivedAt))
                .ToArray();

        var response =
            new ListProjectsResponse(
                items,
                projects.PageNumber,
                projects.PageSize,
                projects.TotalCount,
                projects.TotalPages);

        return Ok(
            response);
    }

    [Authorize(
        Policy = AuthorizationPolicyNames.TenantAccess)]
    [HttpGet("{projectPublicId:guid}")]
    [ProducesResponseType<GetProjectByPublicIdResponse>(
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ErrorResponse>(
        StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ErrorResponse>(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErrorResponse>(
        StatusCodes.Status404NotFound)]
    [ProducesResponseType<ErrorResponse>(
        StatusCodes.Status409Conflict)]
    public async Task<ActionResult<GetProjectByPublicIdResponse>>
        GetByPublicId(
            Guid tenantPublicId,
            Guid projectPublicId,
            CancellationToken cancellationToken)
    {
        var userPublicIdValue =
            User.FindFirst(
                ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(
                userPublicIdValue,
                out var userPublicId))
        {
            return Unauthorized();
        }

        var query =
            new GetProjectByPublicIdQuery(
                tenantPublicId,
                projectPublicId,
                userPublicId);

        var result =
            await _getProjectByPublicIdHandler.HandleAsync(
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

        var project =
            result.Value!;

        var response =
            new GetProjectByPublicIdResponse(
                project.PublicId,
                project.TenantPublicId,
                project.CreatedByUserPublicId,
                project.ResponsibleUserPublicId,
                project.Name,
                project.Description,
                project.Status,
                project.DueDate,
                project.CreatedAt,
                project.UpdatedAt,
                project.ArchivedAt);

        return Ok(
            response);
    }

    [Authorize(
        Policy = AuthorizationPolicyNames.TenantAccess)]
    [HttpPut("{projectPublicId:guid}")]
    [ProducesResponseType<UpdateProjectResponse>(
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ErrorResponse>(
        StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ErrorResponse>(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErrorResponse>(
        StatusCodes.Status404NotFound)]
    [ProducesResponseType<ErrorResponse>(
        StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UpdateProjectResponse>> Update(
        Guid tenantPublicId,
        Guid projectPublicId,
        [FromBody] UpdateProjectRequest request,
        CancellationToken cancellationToken)
    {
        var userPublicIdValue =
            User.FindFirst(
                ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(
                userPublicIdValue,
                out var userPublicId))
        {
            return Unauthorized();
        }

        var command =
            new UpdateProjectCommand(
                tenantPublicId,
                projectPublicId,
                userPublicId,
                request.Name,
                request.Description,
                request.DueDate);

        var result =
            await _updateProjectHandler.HandleAsync(
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
                error == ProjectErrors.Archived)
            {
                return Conflict(
                    errorResponse);
            }

            if (error == UserErrors.Inactive ||
                error == ProjectErrors.UpdateNotAllowed)
            {
                return StatusCode(
                    StatusCodes.Status403Forbidden,
                    errorResponse);
            }

            return BadRequest(
                errorResponse);
        }

        var project =
            result.Value!;

        var response =
            new UpdateProjectResponse(
                project.PublicId,
                project.TenantPublicId,
                project.CreatedByUserPublicId,
                project.ResponsibleUserPublicId,
                project.Name,
                project.Description,
                project.Status,
                project.DueDate,
                project.CreatedAt,
                project.UpdatedAt,
                project.ArchivedAt);

        return Ok(
            response);
    }

    [Authorize(
        Policy = AuthorizationPolicyNames.TenantAccess)]
    [HttpPatch("{projectPublicId:guid}/start")]
    [ProducesResponseType<ProjectStatusResponse>(
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ErrorResponse>(
        StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ErrorResponse>(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErrorResponse>(
        StatusCodes.Status404NotFound)]
    [ProducesResponseType<ErrorResponse>(
        StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProjectStatusResponse>> Start(
        Guid tenantPublicId,
        Guid projectPublicId,
        CancellationToken cancellationToken)
    {
        var userPublicIdValue =
            User.FindFirst(
                ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(
                userPublicIdValue,
                out var userPublicId))
        {
            return Unauthorized();
        }

        var command =
            new StartProjectCommand(
                tenantPublicId,
                projectPublicId,
                userPublicId);

        var result =
            await _startProjectHandler.HandleAsync(
                command,
                cancellationToken);

        if (result.IsFailure)
        {
            return MapProjectStatusError(
                result.Error!);
        }

        return Ok(
            CreateProjectStatusResponse(
                result.Value!));
    }

    [Authorize(
        Policy = AuthorizationPolicyNames.TenantAccess)]
    [HttpPatch("{projectPublicId:guid}/pause")]
    [ProducesResponseType<ProjectStatusResponse>(
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ErrorResponse>(
        StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ErrorResponse>(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErrorResponse>(
        StatusCodes.Status404NotFound)]
    [ProducesResponseType<ErrorResponse>(
        StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProjectStatusResponse>> Pause(
        Guid tenantPublicId,
        Guid projectPublicId,
        [FromBody] PauseProjectRequest request,
        CancellationToken cancellationToken)
    {
        var userPublicIdValue =
            User.FindFirst(
                ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(
                userPublicIdValue,
                out var userPublicId))
        {
            return Unauthorized();
        }

        var command =
            new PauseProjectCommand(
                tenantPublicId,
                projectPublicId,
                userPublicId,
                request.Reason);

        var result =
            await _pauseProjectHandler.HandleAsync(
                command,
                cancellationToken);

        if (result.IsFailure)
        {
            return MapProjectStatusError(
                result.Error!);
        }

        return Ok(
            CreateProjectStatusResponse(
                result.Value!));
    }

    [Authorize(
        Policy = AuthorizationPolicyNames.TenantAccess)]
    [HttpPatch("{projectPublicId:guid}/resume")]
    [ProducesResponseType<ProjectStatusResponse>(
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ErrorResponse>(
        StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ErrorResponse>(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErrorResponse>(
        StatusCodes.Status404NotFound)]
    [ProducesResponseType<ErrorResponse>(
        StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProjectStatusResponse>> Resume(
        Guid tenantPublicId,
        Guid projectPublicId,
        [FromBody] ResumeProjectRequest request,
        CancellationToken cancellationToken)
    {
        var userPublicIdValue =
            User.FindFirst(
                ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(
                userPublicIdValue,
                out var userPublicId))
        {
            return Unauthorized();
        }

        var command =
            new ResumeProjectCommand(
                tenantPublicId,
                projectPublicId,
                userPublicId,
                request.NewDueDate);

        var result =
            await _resumeProjectHandler.HandleAsync(
                command,
                cancellationToken);

        if (result.IsFailure)
        {
            return MapProjectStatusError(
                result.Error!);
        }

        return Ok(
            CreateProjectStatusResponse(
                result.Value!));
    }

    private ActionResult<ProjectStatusResponse>
        MapProjectStatusError(
            Error error)
    {
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
            error == ProjectErrors.InvalidStatusTransition ||
            error == ProjectErrors.Archived)
        {
            return Conflict(
                errorResponse);
        }

        if (error == UserErrors.Inactive ||
            error == ProjectErrors.StatusChangeNotAllowed)
        {
            return StatusCode(
                StatusCodes.Status403Forbidden,
                errorResponse);
        }

        return BadRequest(
            errorResponse);
    }

    private static ProjectStatusResponse
        CreateProjectStatusResponse(
            ProjectStatusResult project)
    {
        return new ProjectStatusResponse(
            project.PublicId,
            project.TenantPublicId,
            project.Status,
            project.DueDate,
            project.UpdatedAt,
            project.ArchivedAt);
    }
}