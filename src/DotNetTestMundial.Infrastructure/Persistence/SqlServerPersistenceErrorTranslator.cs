using DotNetTestMundial.Application.Abstractions.Persistence;
using DotNetTestMundial.Domain.Common;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace DotNetTestMundial.Infrastructure.Persistence;

public sealed class SqlServerPersistenceErrorTranslator : IPersistenceErrorTranslator
{
    public Error? Translate(DbUpdateException exception) => exception switch
    {
        DbUpdateConcurrencyException => PersistenceErrors.ConcurrentChange,
        { InnerException: SqlException { Number: 2601 or 2627 or 547 or 515 or 2628 or 8152 } } =>
            PersistenceErrors.ConstraintViolation,
        _ => null
    };
}
