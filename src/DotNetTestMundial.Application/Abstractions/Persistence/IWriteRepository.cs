// Responsabilidad del archivo: Declara operaciones que preparan altas, cambios y eliminaciones.
// Relación en el sistema: Los Commands usan este puerto y Unit of Work confirma todas las escrituras juntas.
using DotNetTestMundial.Domain.Common;

namespace DotNetTestMundial.Application.Abstractions.Persistence;

public interface IWriteRepository<TEntity> where TEntity : Entity
{
    void Add(TEntity entity);
    // Updates scalar properties only; new children must be added through their repository.
    void Update(TEntity entity);
    void Remove(TEntity entity);
}
