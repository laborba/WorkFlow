using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WorkFlow.API.Authorization;
using WorkFlow.API.Contracts.Common;
using WorkFlow.API.Contracts.Projects;
using WorkFlow.Application.Projects;
using WorkFlow.Application.Projects.AddProjectMember;
using WorkFlow.Application.Projects.ListProjectMembers;
using WorkFlow.Application.Projects.RemoveProjectMember;
using WorkFlow.Application.Tenants;
using WorkFlow.Application.Users;

namespace WorkFlow.API.Controllers;

[ApiController]
[Route(
    "api/tenants/{tenantPublicId:guid}/projects/{projectPublicId:guid}/members")]
public sealed class ProjectMembersController : ControllerBase
{
    private readonly AddProjectMemberHandler
        _addProjectMemberHandler;

    private readonly ListProjectMembersHandler
        _listProjectMembersHandler;

    private readonly RemoveProjectMemberHandler
        _removeProjectMemberHandler;

    public ProjectMembersController(
        AddProjectMemberHandler addProjectMemberHandler,
        ListProjectMembersHandler listProjectMembersHandler,
        RemoveProjectMemberHandler removeProjectMemberHandler)
    {
        _addProjectMemberHandler =
            addProjectMemberHandler;

        _listProjectMembersHandler =
            listProjectMembersHandler;

        _removeProjectMemberHandler =
            removeProjectMemberHandler;
    }

    [Authorize(
        Policy = AuthorizationPolicyNames.TenantAccess)]
    [HttpPost]
    [ProducesResponseType<AddProjectMemberResponse>(
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
    public async Task<ActionResult<AddProjectMemberResponse>> Add(
        Guid tenantPublicId,
        Guid projectPublicId,
        [FromBody] AddProjectMemberRequest request,
        CancellationToken cancellationToken)
    {
        var userPublicIdValue =
            User.FindFirst(
                ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(
                userPublicIdValue,
                out var requestedByUserPublicId))
        {
            return Unauthorized();
        }

        var command =
            new AddProjectMemberCommand(
                tenantPublicId,
                projectPublicId,
                requestedByUserPublicId,
                request.UserPublicId);

        var result =
            await _addProjectMemberHandler.HandleAsync(
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
                error == ProjectErrors.Archived ||
                error == ProjectMemberErrors.AlreadyActive)
            {
                return Conflict(
                    errorResponse);
            }

            if (error == UserErrors.Inactive ||
                error == ProjectMemberErrors.AddNotAllowed)
            {
                return StatusCode(
                    StatusCodes.Status403Forbidden,
                    errorResponse);
            }

            return BadRequest(
                errorResponse);
        }

        var member =
            result.Value!;

        var response =
            new AddProjectMemberResponse(
                member.ProjectPublicId,
                member.UserPublicId,
                member.AddedByUserPublicId,
                member.AddedAt);

        return StatusCode(
            StatusCodes.Status201Created,
            response);
    }

    [Authorize(
        Policy = AuthorizationPolicyNames.TenantAccess)]
    [HttpGet]
    [ProducesResponseType<ListProjectMembersResponse>(
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
    public async Task<ActionResult<ListProjectMembersResponse>> List(
        Guid tenantPublicId,
        Guid projectPublicId,
        [FromQuery] ListProjectMembersRequest request,
        CancellationToken cancellationToken)
    {
        var userPublicIdValue =
            User.FindFirst(
                ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(
                userPublicIdValue,
                out var requestedByUserPublicId))
        {
            return Unauthorized();
        }

        var query =
            new ListProjectMembersQuery(
                tenantPublicId,
                projectPublicId,
                requestedByUserPublicId,
                request.PageNumber,
                request.PageSize,
                request.Search);

        var result =
            await _listProjectMembersHandler.HandleAsync(
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

        var members =
            result.Value!;

        var response =
            new ListProjectMembersResponse(
                members.Items
                    .Select(member =>
                        new ProjectMemberListItemResponse(
                            member.UserPublicId,
                            member.Name,
                            member.Email,
                            member.Role,
                            member.AddedAt,
                            member.AddedByUserPublicId))
                    .ToArray(),
                members.PageNumber,
                members.PageSize,
                members.TotalCount,
                members.TotalPages);

        return Ok(
            response);
    }

    [Authorize(
        Policy = AuthorizationPolicyNames.TenantAccess)]
    [HttpDelete("{userPublicId:guid}")]
    [ProducesResponseType<RemoveProjectMemberResponse>(
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
    public async Task<ActionResult<RemoveProjectMemberResponse>> Remove(
        Guid tenantPublicId,
        Guid projectPublicId,
        Guid userPublicId,
        CancellationToken cancellationToken)
    {
        var userPublicIdValue =
            User.FindFirst(
                ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(
                userPublicIdValue,
                out var requestedByUserPublicId))
        {
            return Unauthorized();
        }

        var command =
            new RemoveProjectMemberCommand(
                tenantPublicId,
                projectPublicId,
                requestedByUserPublicId,
                userPublicId);

        var result =
            await _removeProjectMemberHandler.HandleAsync(
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
                error == ProjectMemberErrors.NotActive)
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
                error == ProjectMemberErrors.RemoveNotAllowed)
            {
                return StatusCode(
                    StatusCodes.Status403Forbidden,
                    errorResponse);
            }

            return BadRequest(
                errorResponse);
        }

        var removedMember =
            result.Value!;

        var response =
            new RemoveProjectMemberResponse(
                removedMember.ProjectPublicId,
                removedMember.UserPublicId,
                removedMember.RemovedAt);

        return Ok(
            response);
    }
}