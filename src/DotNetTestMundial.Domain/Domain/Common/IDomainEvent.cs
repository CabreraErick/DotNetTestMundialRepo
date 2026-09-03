namespace DotNetTestMundial.Domain.Common;

public interface IDomainEvent
{
    DateTime OccurredAt {get;}
}