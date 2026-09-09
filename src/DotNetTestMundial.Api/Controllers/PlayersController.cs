// Responsabilidad del archivo: Adapta el recurso HTTP de jugadores a Commands y Queries.
// Relación en el sistema: Mapea Result a códigos HTTP y deja las reglas en Domain, la coordinación en Application y SQL en Infrastructure.
using DotNetTestMundial.Application.Players.CreatePlayer;
using DotNetTestMundial.Application.Players.GetPlayers;
using DotNetTestMundial.Application.Players.Mutations;
using DotNetTestMundial.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace DotNetTestMundial.Api.Controllers;

[ApiController]
[Route("api/players")]
public sealed class PlayersController(
    CreatePlayerCommandHandler createHandler,
    GetPlayersQueryHandler getPlayersHandler,
    GetPlayerByIdQueryHandler getByIdHandler,
    UpdatePlayerCommandHandler updateHandler,
    PatchPlayerCommandHandler patchHandler,
    DeletePlayerCommandHandler deleteHandler) : ControllerBase
{
    public sealed record CreatePlayerRequest(Guid TeamId, string? Name, int JerseyNumber);
    public sealed record UpdatePlayerRequest(string? Name, int JerseyNumber);
    public sealed record PatchPlayerRequest(string? Name, int? JerseyNumber);

    /// <summary>Registers an active player and persists an idempotent HTTP 201 response.</summary>
    [HttpPost]
    public async Task<IActionResult> Create(
        CreatePlayerRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await createHandler.HandleAsync(
            new(request.TeamId, request.Name, request.JerseyNumber, idempotencyKey),
            cancellationToken);
        if (result.IsFailure)
            return MapError(result.Error!);

        var response = result.Value;
        Response.Headers.Location = $"/api/players/{response.PlayerId}";
        return new ContentResult
        {
            StatusCode = response.StatusCode,
            ContentType = "application/json; charset=utf-8",
            Content = response.ResponseBody
        };
    }

    /// <summary>Returns a filtered and paginated player projection produced by Dapper.</summary>
    [HttpGet]
    public async Task<IActionResult> GetPage(
        [FromQuery] string? search,
        [FromQuery] Guid? teamId,
        [FromQuery] bool? isActive,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string sortBy = "name",
        [FromQuery] string sortDirection = "asc",
        CancellationToken cancellationToken = default)
    {
        var result = await getPlayersHandler.HandleAsync(
            new(search, teamId, isActive, pageNumber, pageSize, sortBy, sortDirection),
            cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : MapError(result.Error!);
    }

    /// <summary>Returns one player through the Dapper read path.</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await getByIdHandler.HandleAsync(new(id), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : MapError(result.Error!);
    }

    /// <summary>Replaces name and jersey number while preserving team and active state.</summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id, UpdatePlayerRequest request, CancellationToken cancellationToken)
    {
        var result = await updateHandler.HandleAsync(
            new(id, request.Name, request.JerseyNumber), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : MapError(result.Error!);
    }

    /// <summary>Changes only the supplied player fields.</summary>
    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> Patch(
        Guid id, PatchPlayerRequest request, CancellationToken cancellationToken)
    {
        var result = await patchHandler.HandleAsync(
            new(id, request.Name, request.JerseyNumber), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : MapError(result.Error!);
    }

    /// <summary>Performs a repeatable logical deletion that preserves tournament history.</summary>
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
