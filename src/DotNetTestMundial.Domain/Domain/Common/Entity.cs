// Responsabilidad del archivo: Proporciona identidad y colección de eventos a las entidades del dominio.
// Relación en el sistema: Team, Player, Match y Goal heredan esta base sin depender de Infrastructure.
namespace DotNetTestMundial.Domain.Common;

public abstract class Entity
{
    private readonly List<IDomainEvent> _domainEvents = new();

    public Guid Id { get; protected set; }

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    protected Entity()
    {
        Id = Guid.NewGuid();
    }

    protected void AddDomainEvent(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}

/*
    Guid: evita que la identidad dependa de una secuenca de base de datos
*/
