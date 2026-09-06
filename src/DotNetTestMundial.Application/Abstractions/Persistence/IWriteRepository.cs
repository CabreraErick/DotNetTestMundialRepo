using DotNetTestMundial.Domain.Common;

namespace DotNetTestMundial.Application.Abstractions.Persistence;

public interface IWriteRepository<TEntity> where TEntity : Entity
{
    void Add(TEntity entity);
    // Updates scalar properties only; new children must be added through their repository.
    void Update(TEntity entity);
    void Remove(TEntity entity);
}
