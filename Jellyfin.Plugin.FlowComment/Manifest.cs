using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.FlowComment
{
    public class ManifestData
    {
        public string? VideoId { get; set; }
    }

    public class ManifestManager
    {
        /// <summary>
        /// Get path to data file of specified item.
        /// </summary>
        static public string GetPath(BaseItem item)
        {
            var folder = Path.Combine(item.ContainingFolderPath, "flowcomment");
            var filename = Path.GetFileNameWithoutExtension(item.Path);
            filename += "-" + "manifest.json";

            return Path.Combine(folder, filename);
        }

        /// <summary>
        /// Load data from file if exists, otherwise return empty data.
        /// </summary>
        static async public Task<ManifestData> GetManifest(BaseItem item)
        {
            var path = GetPath(item);
            if (!File.Exists(path))
            {
                return new ManifestData();
            }

            var json = await File.ReadAllTextAsync(path).ConfigureAwait(false);
            ManifestData? manifest = JsonSerializer.Deserialize<ManifestData>(json);
            return manifest!;
        }

        /// <summary>
        /// Receive VideoId and save
        /// </summary>
        static async public Task SetVideoId(BaseItem item, string videoId)
        {
            var path = GetPath(item);

            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var data = new ManifestData() { VideoId = videoId };
            await File.WriteAllTextAsync(path, JsonSerializer.Serialize(data));
        }
    }

}

