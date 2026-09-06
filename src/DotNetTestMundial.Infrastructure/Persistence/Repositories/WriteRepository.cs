using DotNetTestMundial.Application.Abstractions.Persistence;
using DotNetTestMundial.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace DotNetTestMundial.Infrastructure.Persistence.Repositories;

public sealed class WriteRepository<TEntity>(TournamentDbContext context) : IWriteRepository<TEntity> where TEntity : Entity
{
    public void Add(TEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        context.Set<TEntity>().Add(entity);
    }

    public void Update(TEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        context.Entry(entity).State = EntityState.Modified;
    }

    public void Remove(TEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        context.Entry(entity).State = EntityState.Deleted;
    }
}
