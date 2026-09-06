using DotNetTestMundial.Domain.Common;

namespace DotNetTestMundial.Application.Abstractions.Messaging;

public interface ICommandHandler<in TCommand, TResult>
{
    Task<Result<TResult>> HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}
