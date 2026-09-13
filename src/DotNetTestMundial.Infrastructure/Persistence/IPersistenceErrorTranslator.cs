// Responsabilidad del archivo: Abstrae la conversión de errores técnicos de EF a errores Result.
// Relación en el sistema: UnitOfWork depende del contrato y la implementación SQL Server interpreta códigos del proveedor.
using DotNetTestMundial.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace DotNetTestMundial.Infrastructure.Persistence;

public interface IPersistenceErrorTranslator
{
    // Return null for technical errors that must propagate to the application error boundary.
    Error? Translate(DbUpdateException exception);
}
