using CustomGenerics;

namespace Domain.Entities.Plugin;

public interface IRequestHandlerablePlugin<TRequest, TResponse> : IPlugin, IRequestHandlerableAsync<TRequest, TResponse> { }