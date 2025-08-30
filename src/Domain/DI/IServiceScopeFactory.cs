namespace Domain.DI;

public interface IServiceScopeFactory
{
    IServiceScope CreateScope();
}
