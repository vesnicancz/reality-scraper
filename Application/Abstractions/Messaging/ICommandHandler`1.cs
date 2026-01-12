using RealityScraper.SharedKernel;

namespace RealityScraper.Application.Abstractions.Messaging;

public interface ICommandHandler<in TCommand>
	where TCommand : ICommand
{
	Task<Result> Handle(TCommand command, CancellationToken cancellationToken);
}