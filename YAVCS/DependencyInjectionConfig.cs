using Microsoft.Extensions.DependencyInjection;
using YAVCS.Services;
using YAVCS.Services.Contracts;

namespace YAVCS;

public static class DependencyInjectionConfig
{
    public static IServiceProvider Configure()
    {
        return new ServiceCollection()
            .AddSingleton<INavigatorService,NavigatorService>()
            .AddSingleton<IConfigService,ConfigService>()
            .BuildServiceProvider();
    }
}