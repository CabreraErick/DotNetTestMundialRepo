// Responsabilidad del archivo: Centraliza errores invariantes de equipos, jugadores, partidos y goles.
// Relación en el sistema: Las entidades los retornan mediante Result y las pruebas verifican sus códigos.
namespace DotNetTestMundial.Domain.Common;

public static class DomainErrors
{
    public static readonly Error TeamNameRequired = new("Team.NameRequired", "Team name is required.");
    public static readonly Error TeamShortNameRequired = new("Team.ShortNameRequired", "Team short name is required.");
    public static readonly Error TeamNameTooLong = new("Team.NameTooLong", "Team name cannot exceed 100 characters.");
    public static readonly Error TeamShortNameTooLong = new("Team.ShortNameTooLong", "Team short name cannot exceed 10 characters.");
    public static readonly Error TeamRequired = new("Player.TeamRequired", "Team is required.");
    public static readonly Error PlayerRequired = new("Player.Required", "Player is required.");
    public static readonly Error PlayerNameRequired = new("Player.NameRequired", "Player name is required.");
    public static readonly Error PlayerNameTooLong = new("Player.NameTooLong", "Player name cannot exceed 120 characters.");
    public static readonly Error InvalidJerseyNumber = new("Player.InvalidJerseyNumber", "Jersey number must be greater than zero.");
    public static readonly Error PlayerTeamMismatch = new("Team.PlayerTeamMismatch", "The player does not belong to this team.");
    public static readonly Error InactivePlayer = new("Goal.InactivePlayer", "An inactive player cannot register a goal.", ErrorType.Conflict);
    public static readonly Error HomeTeamRequired = new("Match.HomeTeamRequired", "Home team is required.");
    public static readonly Error AwayTeamRequired = new("Match.AwayTeamRequired", "Away team is required.");
    public static readonly Error SameTeams = new("Match.SameTeams", "A team cannot play against itself.");
    public static readonly Error ScheduledAtRequired = new("Match.ScheduledAtRequired", "A scheduled date is required.");
    public static readonly Error MatchNotScheduled = new("Match.NotScheduled", "Only scheduled matches can be changed.", ErrorType.Conflict);
    public static readonly Error NegativeScore = new("Match.NegativeScore", "Scores cannot be negative.");
    public static readonly Error ScoreMismatch = new("Match.ScoreMismatch", "The score must match the registered goals for each team.");
    public static readonly Error GoalRequired = new("Goal.Required", "Goal is required.");
    public static readonly Error MatchRequired = new("Goal.MatchRequired", "Match is required.");
    public static readonly Error InvalidGoalMinute = new("Goal.InvalidMinute", "Goal minute must be between 1 and 120.");
    public static readonly Error GoalMatchMismatch = new("Match.GoalMatchMismatch", "The goal does not belong to this match.");
    public static readonly Error ScorerTeamMismatch = new("Match.ScorerTeamMismatch", "The scorer must belong to a participating team.");
    public static readonly Error DuplicateGoal = new("Match.DuplicateGoal", "This goal is already registered.", ErrorType.Conflict);
}
