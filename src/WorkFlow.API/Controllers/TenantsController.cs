using Microsoft.AspNetCore.Mvc;
using WorkFlow.API.Contracts.Common;
using WorkFlow.API.Contracts.Tenants;
using WorkFlow.Application.Tenants.CreateTenant;
using WorkFlow.Application.Tenants.GetTenantByPublicId;
using WorkFlow.Application.Tenants.ChangeTenantStatus;
using WorkFlow.Application.Tenants;

namespace WorkFlow.API.Controllers;

[ApiController]
[Route("api/tenants")]
public sealed class TenantsController : ControllerBase
{
    private readonly CreateTenantHandler
        _createTenantHandler;

    private readonly GetTenantByPublicIdHandler
        _getTenantByPublicIdHandler;

    private readonly ChangeTenantStatusHandler 
        _changeTenantStatusHandler;

    public TenantsController(
        CreateTenantHandler createTenantHandler,
        GetTenantByPublicIdHandler getTenantByPublicIdHandler,
        ChangeTenantStatusHandler changeTenantStatusHandler)
    {
        _createTenantHandler = createTenantHandler;
        _getTenantByPublicIdHandler = getTenantByPublicIdHandler;
        _changeTenantStatusHandler = changeTenantStatusHandler;
    }

    [HttpPost]
    [ProducesResponseType<CreateTenantResponse>(
        StatusCodes.Status201Created)]
    [ProducesResponseType<ErrorResponse>(
        StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CreateTenantResponse>> Create(
        CreateTenantRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateTenantCommand(
            request.Name,
            request.RegistrationNumber,
            request.Email,
            request.Phone);

        var result =
            await _createTenantHandler.HandleAsync(
                command,
                cancellationToken);

        if (result.IsFailure)
        {
            var error = result.Error!;

            return Conflict(
                new ErrorResponse(
                    error.Code,
                    error.Message));
        }

        var createdTenant = result.Value!;

        var response = new CreateTenantResponse(
            createdTenant.PublicId);

        return Created(
            $"/api/tenants/{response.PublicId}",
            response);
    }

    [HttpGet("{publicId:guid}")]
    [ProducesResponseType<GetTenantResponse>(
        StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorResponse>(
        StatusCodes.Status404NotFound)]
    [ProducesResponseType<ErrorResponse>(
        StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GetTenantResponse>> GetByPublicId(
    Guid publicId,
    CancellationToken cancellationToken)
    {
        var query =
            new GetTenantByPublicIdQuery(
                publicId);

        var result =
            await _getTenantByPublicIdHandler.HandleAsync(
                query,
                cancellationToken);

        if (result.IsFailure)
        {
            var error = result.Error!;

            return NotFound(
                new ErrorResponse(
                    error.Code,
                    error.Message));
        }

        var tenant = result.Value!;

        var response = new GetTenantResponse(
            tenant.PublicId,
            tenant.Name,
            tenant.RegistrationNumber,
            tenant.Email,
            tenant.Phone,
            tenant.IsActive,
            tenant.CreatedAt,
            tenant.UpdatedAt);

        return Ok(response);
    }

    [HttpPatch("{publicId:guid}/status")]
    public async Task<ActionResult<ChangeTenantStatusResponse>> ChangeStatus(
    Guid publicId,
    [FromBody] ChangeTenantStatusRequest request,
    CancellationToken cancellationToken)
    {
        var command = new ChangeTenantStatusCommand(
            publicId,
            request.IsActive);

        var result = await _changeTenantStatusHandler.HandleAsync(
            command,
            cancellationToken);

        if (result.IsFailure)
        {
            var errorResponse = new ErrorResponse(
                result.Error.Code,
                result.Error.Message);

            if (result.Error == TenantErrors.NotFound)
            {
                return NotFound(errorResponse);
            }

            return BadRequest(errorResponse);
        }

        var response = new ChangeTenantStatusResponse(
            result.Value.PublicId,
            result.Value.IsActive);

        return Ok(response);
    }
}