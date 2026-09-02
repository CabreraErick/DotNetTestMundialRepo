namespace DotNetTestMundial.Domain.Common;

public abstract class Entity
{
    private readonly List<object> _domainEvents = new ();

    public Guid Id {get; protected set;}

    public IReadOnlyCollection<object> DomainEvents => _domainEvents.AsReadOnly();
    protected Entity()
    {
        Id = Guid.NewGuid();
    }

    protected void AddDomainEvent(object domainEvent)
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