namespace DotNetTestMundial.Application.Teams.CreateTeam;

public sealed record CreateTeamCommand(string? Name, string? ShortName, string? IdempotencyKey);

public sealed record CreateTeamOutcome(Guid TeamId, int StatusCode, string ResponseBody, bool IsReplay);
