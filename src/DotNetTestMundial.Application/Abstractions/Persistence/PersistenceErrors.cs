// Responsabilidad del archivo: Centraliza errores esperados de restricciones y concurrencia.
// Relación en el sistema: Unit of Work los devuelve como Result y API los transforma en HTTP 409.
using DotNetTestMundial.Domain.Common;

namespace DotNetTestMundial.Application.Abstractions.Persistence;

public static class PersistenceErrors
{
    public static readonly Error ConcurrentChange = new(
        "Persistence.ConcurrentChange", "The record was changed or removed. Reload it before retrying.", ErrorType.Conflict);
    public static readonly Error ConstraintViolation = new(
        "Persistence.ConstraintViolation", "The change conflicts with existing data or a required relationship.", ErrorType.Conflict);
}
