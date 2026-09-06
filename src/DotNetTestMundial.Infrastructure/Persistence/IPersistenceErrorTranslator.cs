using DotNetTestMundial.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace DotNetTestMundial.Infrastructure.Persistence;

public interface IPersistenceErrorTranslator
{
    // Return null for technical errors that must propagate to the application error boundary.
    Error? Translate(DbUpdateException exception);
}
