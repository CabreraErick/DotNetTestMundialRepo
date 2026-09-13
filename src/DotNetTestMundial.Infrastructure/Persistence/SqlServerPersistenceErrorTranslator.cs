// Responsabilidad del archivo: Traduce concurrencia y restricciones conocidas de SQL Server.
// Relación en el sistema: Impide exponer detalles internos y entrega conflictos tipados a Application.
using DotNetTestMundial.Application.Abstractions.Persistence;
using DotNetTestMundial.Application.Matches.Mutations;
using DotNetTestMundial.Application.Players.Mutations;
using DotNetTestMundial.Application.Teams.Mutations;
using DotNetTestMundial.Domain.Common;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace DotNetTestMundial.Infrastructure.Persistence;

public sealed class SqlServerPersistenceErrorTranslator : IPersistenceErrorTranslator
{
    public Error? Translate(DbUpdateException exception)
    {
        if (exception is DbUpdateConcurrencyException)
            return PersistenceErrors.ConcurrentChange;
        if (exception.InnerException is not SqlException sqlException)
            return null;
        if (sqlException.Number == 51001)
            return MatchMutationErrors.ScheduleConflict;
        if (sqlException.Number is 2601 or 2627)
        {
            if (sqlException.Message.Contains("UX_Teams_Name", StringComparison.Ordinal))
                return TeamMutationErrors.NameAlreadyExists;
            if (sqlException.Message.Contains("UX_Teams_ShortName", StringComparison.Ordinal))
                return TeamMutationErrors.ShortNameAlreadyExists;
            if (sqlException.Message.Contains("UX_Players_TeamId_JerseyNumber", StringComparison.Ordinal))
                return PlayerMutationErrors.JerseyNumberAlreadyAssigned;
        }
        return sqlException.Number is 2601 or 2627 or 547 or 515 or 2628 or 8152
            ? PersistenceErrors.ConstraintViolation
            : null;
    }
}
