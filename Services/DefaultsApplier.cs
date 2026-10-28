using Jellyfin.Data;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Enums;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging;

namespace JellyDefaults.Services;

// Writes the plugin's template (library order + My Media excludes) into user configs.
public class DefaultsApplier
{
    private readonly IUserManager _userManager;
    private readonly ILogger<DefaultsApplier> _logger;

    public DefaultsApplier(IUserManager userManager, ILogger<DefaultsApplier> logger)
    {
        _userManager = userManager;
        _logger = logger;
    }

    public static bool HasTemplate => Plugin.Instance!.Configuration.OrderedViews.Length > 0;

    // A user who never reordered has an empty OrderedViews (alphabetical default).
    public static bool IsCustomized(User user) =>
        user.GetPreferenceValues<Guid>(PreferenceKind.OrderedViews).Length > 0;

    public IReadOnlyList<User> GetTargets(bool includeCustomized) =>
        _userManager.GetUsers().Where(u => includeCustomized || !IsCustomized(u)).ToList();

    public async Task ApplyAsync(Guid userId)
    {
        var config = Plugin.Instance!.Configuration;
        var user = _userManager.GetUserById(userId)
            ?? throw new ArgumentException($"User {userId} not found");

        user.SetPreference(PreferenceKind.OrderedViews, config.OrderedViews);
        user.SetPreference(PreferenceKind.MyMediaExcludes, config.MyMediaExcludes);
        await _userManager.UpdateUserAsync(user).ConfigureAwait(false);
        _logger.LogInformation("Applied default library order to {User}", user.Username);
    }
}
