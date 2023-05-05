using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.FlowComment.Configuration;


/// <summary>
/// Plugin configuration.
/// </summary>
public class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PluginConfiguration"/> class.
    /// </summary>
    public PluginConfiguration()
    {
        NiconicoSession = "";
    }

    /// <summary>
    /// Gets or sets Session id for niconico.
    /// </summary>
    public string NiconicoSession { get; set; }
}
