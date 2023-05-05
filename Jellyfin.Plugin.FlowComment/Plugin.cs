using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using Jellyfin.Plugin.FlowComment.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.FlowComment;

/// <summary>
/// The main plugin.
/// </summary>
public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Plugin"/> class.
    /// </summary>
    public Plugin(
        IApplicationPaths applicationPaths,
        IXmlSerializer xmlSerializer,
        ILogger<Plugin> logger,
        IServerConfigurationManager configurationManager)
        : base(applicationPaths, xmlSerializer)
    {
        Instance = this;

        if (!string.IsNullOrWhiteSpace(applicationPaths.WebPath))
        {
            var indexFile = Path.Combine(applicationPaths.WebPath, "index.html");
            if (File.Exists(indexFile))
            {
                string indexContents = File.ReadAllText(indexFile);
                string basePath = string.Empty;

                // Get base path from network config
                try
                {
                    var networkConfig = configurationManager.GetConfiguration("network");
                    var configType = networkConfig.GetType();
                    var basePathField = configType.GetProperty("BaseUrl");
                    var confBasePath = basePathField?.GetValue(networkConfig)?.ToString()?.Trim('/');

                    if (!string.IsNullOrEmpty(confBasePath))
                    {
                        basePath = "/" + confBasePath.ToString();
                    }
                }
                catch (Exception e)
                {
                    logger.LogError("Unable to get base path from config, using '/': {0}", e);
                }

                // Don't run if script already exists
                string scriptReplace = "<script plugin=\"FlowComment\".*?></script>";
                string scriptElement = $"<script plugin=\"FlowComment\" version=\"1.0.0.0\" src=\"{basePath}/FlowComment/ClientScript\"></script>";

                if (!indexContents.Contains(scriptElement, StringComparison.Ordinal))
                {
                    logger.LogInformation("Attempting to inject flowcomment script code in {0}", indexFile);

                    // Replace old FlowComment scrips
                    indexContents = Regex.Replace(indexContents, scriptReplace, string.Empty);

                    // Insert script last in body
                    int bodyClosing = indexContents.LastIndexOf("</body>", StringComparison.Ordinal);
                    if (bodyClosing != -1)
                    {
                        indexContents = indexContents.Insert(bodyClosing, scriptElement);

                        try
                        {
                            File.WriteAllText(indexFile, indexContents);
                            logger.LogInformation("Finished injecting flowcomment script code in {0}", indexFile);
                        }
                        catch (Exception e)
                        {
                            logger.LogError("Encountered exception while writing to {0}: {1}", indexFile, e);
                        }
                    }
                    else
                    {
                        logger.LogInformation("Could not find closing body tag in {0}", indexFile);
                    }
                }
                else
                {
                    logger.LogInformation("Found client script injected in {0}", indexFile);
                }
            }
        }
    }

    /// <inheritdoc />
    public override string Name => "FlowComment";

    /// <inheritdoc />
    public override Guid Id => Guid.Parse("c7976193-ee81-46a0-9f48-b2527a9b06dd");

    /// <summary>
    /// Gets the current plugin instance.
    /// </summary>
    public static Plugin? Instance { get; private set; }

    /// <inheritdoc />
    public IEnumerable<PluginPageInfo> GetPages()
    {
        return new[]
        {
            new PluginPageInfo
            {
                Name = this.Name,
                EmbeddedResourcePath = string.Format(CultureInfo.InvariantCulture, "{0}.Configuration.configPage.html", GetType().Namespace)
            }
        };
    }
}
