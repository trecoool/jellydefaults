using System.Text.Json;
using System.Text.RegularExpressions;
using Jellyfin.Database.Implementations.Entities;
using JellyDefaults.Configuration;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging;

namespace JellyDefaults.Services;

// Reads/writes main-tab library sorts in jellyfin-web's server-side prefs ("usersettings", client "emby").
// Tabs store {"SortBy","SortOrder"} JSON under "<lib>-<tab>"; Folder libraries use two "items-<lib>-Folder-sort*" keys.
public partial class SortDefaultsApplier
{
    private const string Client = "emby";
    private const string FolderTab = "Folder";
    private const string DefaultOrder = "Ascending";
    private static readonly Guid PrefsId = "usersettings".GetMD5();

    // jellyfin-web's SortBy when the user never picked one.
    private static readonly Dictionary<string, string> DefaultSortBy = new()
    {
        ["movies"] = "SortName,ProductionYear",
        ["series"] = "SortName",
        ["musicalbums"] = "SortName",
        [FolderTab] = "SortName",
    };

    private readonly IUserManager _userManager;
    private readonly IDisplayPreferencesManager _prefsManager;
    private readonly ILogger<SortDefaultsApplier> _logger;

    public SortDefaultsApplier(IUserManager userManager, IDisplayPreferencesManager prefsManager, ILogger<SortDefaultsApplier> logger)
    {
        _userManager = userManager;
        _prefsManager = prefsManager;
        _logger = logger;
    }

    public static bool HasTemplate => Plugin.Instance!.Configuration.Sorts.Length > 0;

    [GeneratedRegex("^([0-9a-f]{32})-(movies|series|musicalbums)$")]
    private static partial Regex TabKey();

    [GeneratedRegex("^items-([0-9a-f]{32})-Folder-sort(?:by|order)$")]
    private static partial Regex FolderKey();

    // Main-tab sorts this user changed from the default (import source).
    public IReadOnlyList<SortDefault> ReadCustomSorts(Guid userId)
    {
        var prefs = Load(userId);
        var found = new List<(Guid Library, string Tab)>();
        foreach (var key in prefs.Keys)
        {
            var tab = TabKey().Match(key);
            var folder = FolderKey().Match(key);
            if (tab.Success)
            {
                found.Add((Guid.Parse(tab.Groups[1].Value), tab.Groups[2].Value));
            }
            else if (folder.Success)
            {
                found.Add((Guid.Parse(folder.Groups[1].Value), FolderTab));
            }
        }

        return found.Distinct().Select(f => Read(prefs, f.Library, f.Tab)).Where(s => !IsDefault(s)).ToList();
    }

    // Users that would change, with how many template tabs each would get.
    public IReadOnlyList<(User User, int Tabs)> GetTargets(bool includeCustomized) =>
        _userManager.GetUsers()
            .Select(u => (User: u, Tabs: Pending(Load(u.Id), includeCustomized).Count))
            .Where(t => t.Tabs > 0)
            .ToList();

    public void Apply(User user, bool includeCustomized)
    {
        var prefs = Load(user.Id);
        var pending = Pending(prefs, includeCustomized);
        if (pending.Count == 0)
        {
            return;
        }

        pending.ForEach(s => Write(prefs, s));
        _prefsManager.SetCustomItemDisplayPreferences(user.Id, PrefsId, Client, prefs);
        _logger.LogInformation("Applied {Count} default library sort(s) to {User}", pending.Count, user.Username);
    }

    // Template entries that would change this user: all differing ones, or only those still on default.
    private static List<SortDefault> Pending(Dictionary<string, string?> prefs, bool includeCustomized) =>
        Plugin.Instance!.Configuration.Sorts.Where(t =>
        {
            var current = Read(prefs, t.LibraryId, t.Tab);
            var same = current.SortBy == t.SortBy && current.SortOrder == t.SortOrder;
            return !same && (includeCustomized || IsDefault(current));
        }).ToList();

    private static bool IsDefault(SortDefault s) =>
        s.SortBy == DefaultSortBy.GetValueOrDefault(s.Tab) && s.SortOrder == DefaultOrder;

    private Dictionary<string, string?> Load(Guid userId) =>
        new(_prefsManager.ListCustomItemDisplayPreferences(userId, PrefsId, Client));

    // Missing values resolve to jellyfin-web's defaults.
    private static SortDefault Read(Dictionary<string, string?> prefs, Guid libraryId, string tab)
    {
        var id = libraryId.ToString("N");
        string? sortBy;
        string? sortOrder;
        if (tab == FolderTab)
        {
            sortBy = prefs.GetValueOrDefault($"items-{id}-Folder-sortby");
            sortOrder = prefs.GetValueOrDefault($"items-{id}-Folder-sortorder");
        }
        else
        {
            var query = ParseQuery(prefs.GetValueOrDefault($"{id}-{tab}"));
            sortBy = query?.SortBy;
            sortOrder = query?.SortOrder;
        }

        return new SortDefault
        {
            LibraryId = libraryId,
            Tab = tab,
            SortBy = string.IsNullOrEmpty(sortBy) ? DefaultSortBy.GetValueOrDefault(tab, string.Empty) : sortBy,
            SortOrder = string.IsNullOrEmpty(sortOrder) ? DefaultOrder : sortOrder,
        };
    }

    private static void Write(Dictionary<string, string?> prefs, SortDefault s)
    {
        var id = s.LibraryId.ToString("N");
        if (s.Tab == FolderTab)
        {
            prefs[$"items-{id}-Folder-sortby"] = s.SortBy;
            prefs[$"items-{id}-Folder-sortorder"] = s.SortOrder;
        }
        else
        {
            prefs[$"{id}-{s.Tab}"] = JsonSerializer.Serialize(new SortQuery(s.SortBy, s.SortOrder));
        }
    }

    private static SortQuery? ParseQuery(string? json)
    {
        if (string.IsNullOrEmpty(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<SortQuery>(json);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private sealed record SortQuery(string? SortBy, string? SortOrder);
}
