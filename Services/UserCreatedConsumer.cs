using Jellyfin.Data.Events.Users;
using MediaBrowser.Controller.Events;

namespace JellyDefaults.Services;

// Applies the templates (library order, library sorts) to every newly created user.
public class UserCreatedConsumer : IEventConsumer<UserCreatedEventArgs>
{
    private readonly DefaultsApplier _applier;
    private readonly SortDefaultsApplier _sorts;

    public UserCreatedConsumer(DefaultsApplier applier, SortDefaultsApplier sorts)
    {
        _applier = applier;
        _sorts = sorts;
    }

    public async Task OnEvent(UserCreatedEventArgs eventArgs)
    {
        var user = eventArgs.Argument;
        if (DefaultsApplier.HasTemplate)
        {
            await _applier.ApplyAsync(user.Id).ConfigureAwait(false);
        }

        if (SortDefaultsApplier.HasTemplate)
        {
            _sorts.Apply(user, includeCustomized: false);
        }
    }
}
