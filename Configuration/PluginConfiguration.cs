using MediaBrowser.Model.Plugins;

namespace JellyDefaults.Configuration;

// Template copied into users: home library order, views hidden from My Media, library sorts.
public class PluginConfiguration : BasePluginConfiguration
{
    public bool ShowInAdminMenu { get; set; } = true;

    public Guid[] OrderedViews { get; set; } = Array.Empty<Guid>();

    public Guid[] MyMediaExcludes { get; set; } = Array.Empty<Guid>();

    public SortDefault[] Sorts { get; set; } = Array.Empty<SortDefault>();
}
