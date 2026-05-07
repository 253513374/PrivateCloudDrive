using Microsoft.Extensions.DependencyInjection;

namespace PrivateCloudDrive.App.Services;

public static class AppServices
{
    public static IServiceProvider Current { get; private set; } = default!;

    public static void Initialize(IServiceProvider serviceProvider)
    {
        Current = serviceProvider;
    }

    public static T GetRequiredService<T>()
        where T : notnull
    {
        return Current.GetRequiredService<T>();
    }
}
