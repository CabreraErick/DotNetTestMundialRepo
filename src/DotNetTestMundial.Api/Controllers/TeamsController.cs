// Responsabilidad del archivo: Adapta solicitudes HTTP de equipos a Commands y Queries.
// Relación en el sistema: Mapea Result a códigos HTTP y delega toda regla a Application.
using DotNetTestMundial.Application.Teams.CreateTeam;
using DotNetTestMundial.Application.Teams.GetTeams;
using DotNetTestMundial.Application.Teams.Mutations;
using DotNetTestMundial.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace DotNetTestMundial.Api.Controllers;

[ApiController]
[Route("api/teams")]
/// <summary>
/// HTTP adapter for team use cases. It translates transport input to Commands/Queries
/// and Result errors to status codes; business and persistence logic remain outside API.
/// </summary>
public sealed class TeamsController(
    CreateTeamCommandHandler createHandler,
    GetTeamsQueryHandler getTeamsHandler,
    GetTeamByIdQueryHandler getByIdHandler,
    UpdateTeamCommandHandler updateHandler,
    PatchTeamCommandHandler patchHandler,
    DeleteTeamCommandHandler deleteHandler) : ControllerBase
{
    public sealed record CreateTeamRequest(string? Name, string? ShortName);
    public sealed record UpdateTeamRequest(string? Name, string? ShortName);
    public sealed record PatchTeamRequest(string? Name, string? ShortName);

    [HttpPost]
    public async Task<IActionResult> Create(
        CreateTeamRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await createHandler.HandleAsync(
            new CreateTeamCommand(request.Name, request.ShortName, idempotencyKey), cancellationToken);

        if (result.IsFailure)
            return result.Error!.Type switch
            {
                ErrorType.Validation => BadRequest(result.Error),
                ErrorType.NotFound => NotFound(result.Error),
                ErrorType.Conflict => Conflict(result.Error),
                _ => throw new InvalidOperationException("Unsupported error type.")
            };

        var response = result.Value;
        Response.Headers.Location = $"/api/teams/{response.TeamId}";
        return new ContentResult
        {
            StatusCode = response.StatusCode,
            ContentType = "application/json; charset=utf-8",
            Content = response.ResponseBody
        };
    }

    /// <summary>Returns a filtered, sorted page produced by the Dapper read side.</summary>
    [HttpGet]
    public async Task<IActionResult> GetPage(
        [FromQuery] string? search,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string sortBy = "name",
        [FromQuery] string sortDirection = "asc",
        CancellationToken cancellationToken = default)
    {
        var result = await getTeamsHandler.HandleAsync(
            new GetTeamsQuery(search, pageNumber, pageSize, sortBy, sortDirection), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    /// <summary>Returns one team through the Dapper Query path.</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await getByIdHandler.HandleAsync(new(id), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : MapError(result.Error!);
    }

    /// <summary>Replaces all editable team fields and commits once through Unit of Work.</summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id, UpdateTeamRequest request, CancellationToken cancellationToken)
    {
        var result = await updateHandler.HandleAsync(new(id, request.Name, request.ShortName), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : MapError(result.Error!);
    }

    /// <summary>Changes only fields present in the request and preserves the others.</summary>
    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> Patch(
        Guid id, PatchTeamRequest request, CancellationToken cancellationToken)
    {
        var result = await patchHandler.HandleAsync(new(id, request.Name, request.ShortName), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : MapError(result.Error!);
    }

    /// <summary>Deletes an unreferenced team; SQL relationship conflicts become HTTP 409.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await deleteHandler.HandleAsync(new(id), cancellationToken);
        return result.IsSuccess ? NoContent() : MapError(result.Error!);
    }

    private IActionResult MapError(Error error) => error.Type switch
    {
        ErrorType.Validation => BadRequest(error),
        ErrorType.NotFound => NotFound(error),
        ErrorType.Conflict => Conflict(error),
        _ => throw new InvalidOperationException("Unsupported error type.")
    };
}
