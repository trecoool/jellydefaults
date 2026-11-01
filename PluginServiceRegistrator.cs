using Jellyfin.Data.Events.Users;
using JellyDefaults.Services;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Events;
using MediaBrowser.Controller.Plugins;
using Microsoft.Extensions.DependencyInjection;

namespace JellyDefaults;

public class PluginServiceRegistrator : IPluginServiceRegistrator
{
    public void RegisterServices(IServiceCollection services, IServerApplicationHost appHost)
    {
        services.AddSingleton<DefaultsApplier>();
        services.AddScoped<SortDefaultsApplier>();
        services.AddScoped<IEventConsumer<UserCreatedEventArgs>, UserCreatedConsumer>();
    }
}
