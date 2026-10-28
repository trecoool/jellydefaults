using Jellyfin.Data.Events.Users;
using MediaBrowser.Controller.Events;

namespace JellyDefaults.Services;

// Applies the template to every newly created user.
public class UserCreatedConsumer : IEventConsumer<UserCreatedEventArgs>
{
    private readonly DefaultsApplier _applier;

    public UserCreatedConsumer(DefaultsApplier applier)
    {
        _applier = applier;
    }

    public Task OnEvent(UserCreatedEventArgs eventArgs) =>
        DefaultsApplier.HasTemplate ? _applier.ApplyAsync(eventArgs.Argument.Id) : Task.CompletedTask;
}
