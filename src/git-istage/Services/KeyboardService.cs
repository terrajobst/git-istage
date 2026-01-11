using Microsoft.Extensions.DependencyInjection;

namespace GitIStage.Services;

internal sealed class KeyboardService
{
    private readonly IServiceProvider _serviceProvider;

    public KeyboardService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public ConsoleKeyInfo ReadKey()
    {
        var application = _serviceProvider.GetRequiredService<Application>();
        return application.ReadKey();
    }
}