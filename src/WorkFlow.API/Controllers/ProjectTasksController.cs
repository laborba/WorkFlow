using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WorkFlow.API.Authorization;
using WorkFlow.API.Contracts.Common;
using WorkFlow.API.Contracts.ProjectTasks;
using WorkFlow.Application.Projects;
using WorkFlow.Application.ProjectTasks;
using WorkFlow.Application.ProjectTasks.CreateProjectTask;
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

    public ProjectTasksController(
        CreateProjectTaskHandler createProjectTaskHandler)
    {
        _createProjectTaskHandler =
            createProjectTaskHandler;
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
                error == ProjectTaskErrors.CreationBlockedByProjectStatus)
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
}
