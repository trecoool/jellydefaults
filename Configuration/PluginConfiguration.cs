using MediaBrowser.Model.Plugins;

namespace JellyDefaults.Configuration;

// Template copied into user configs: home library order + views hidden from My Media.
public class PluginConfiguration : BasePluginConfiguration
{
    public Guid[] OrderedViews { get; set; } = Array.Empty<Guid>();

    public Guid[] MyMediaExcludes { get; set; } = Array.Empty<Guid>();
}
