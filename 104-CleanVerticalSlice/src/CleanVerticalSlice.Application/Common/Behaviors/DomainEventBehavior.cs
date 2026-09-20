namespace CleanVerticalSlice.Application.Common.Behaviors;

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using CleanVerticalSlice.Application.Common.Interfaces;
using CleanVerticalSlice.Domain.Common;

public class DomainEventBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IPublisher _publisher;
    
    public DomainEventBehavior(IPublisher publisher)
    {
        _publisher = publisher;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var response = await next();
        
        // This is a simplified version. Ideally we collect domain events from DbContext Entities here or directly in SaveChangesAsync
        
        return response;
    }
}
