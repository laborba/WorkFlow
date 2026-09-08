using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WorkFlow.API.Authorization;
using WorkFlow.API.Contracts.Common;
using WorkFlow.API.Contracts.Projects;
using WorkFlow.Application.Projects;
using WorkFlow.Application.Projects.CreateProject;
using WorkFlow.Application.Projects.GetProjectByPublicId;
using WorkFlow.Application.Tenants;
using WorkFlow.Application.Users;

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

    public ProjectsController(
        CreateProjectHandler createProjectHandler,
        GetProjectByPublicIdHandler getProjectByPublicIdHandler)
    {
        _createProjectHandler =
            createProjectHandler;

        _getProjectByPublicIdHandler =
            getProjectByPublicIdHandler;
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
}