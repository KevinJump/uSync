using Microsoft.Extensions.DependencyInjection;

namespace uSync.IntegrationTests;
public class Setup : IDisposable
{
    private bool _disposedValue;
    public IntegrationTestFactory Factory { get; }
    public AsyncServiceScope Scope { get; }

    public IServiceProvider ServiceProvider => Scope.ServiceProvider;

    public Setup()
    {
        Factory = new IntegrationTestFactory();
        Scope = Factory.Services.GetRequiredService<IServiceScopeFactory>().CreateAsyncScope();
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                Factory.CloseDatabase();
            }

            _disposedValue = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
