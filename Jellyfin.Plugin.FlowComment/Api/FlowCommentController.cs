using System;
using System.ComponentModel.DataAnnotations;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Jellyfin.Plugin.FlowComment.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.FlowComment.Api;

/// <summary>
/// Api router.
/// </summary>
[ApiController]
[Route("FlowComment")]
public class FlowCommentController : ControllerBase
{
    private readonly Assembly _assembly;
    private readonly string _flowCommentScriptPath;

    private readonly ILibraryManager _libraryManager;
    private readonly ILogger<FlowCommentController> _logger;
    private readonly IApplicationPaths _appPaths;

    private readonly PluginConfiguration _config;

    /// <summary>
    /// Initializes a new instance of the <see cref="FlowCommentController"/> class.
    /// </summary>
    public FlowCommentController(
        ILibraryManager libraryManager,
        ILogger<FlowCommentController> logger,
        IApplicationPaths appPaths,
        ILibraryMonitor libraryMonitor,
        IMediaEncoder mediaEncoder,
        EncodingHelper encodingHelper)
    {
        _assembly = Assembly.GetExecutingAssembly();
        _flowCommentScriptPath = GetType().Namespace + ".flowcomment.js";

        _libraryManager = libraryManager;
        _logger = logger;
        _appPaths = appPaths;

        _config = Plugin.Instance!.Configuration;
    }

    /// <summary>
    /// Get embedded javascript file for client-side code.
    /// </summary>
    /// <response code="200">Javascript file successfully returned.</response>
    /// <response code="404">File not found.</response>
    /// <returns>The "flowComment.js" embedded file.</returns>
    [HttpGet("ClientScript")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/javascript")]
    public ActionResult GetClientScript()
    {
        var scriptStream = _assembly.GetManifestResourceStream(_flowCommentScriptPath);

        if (scriptStream != null)
        {
            return File(scriptStream, "application/javascript");
        }

        return NotFound();
    }

    [HttpGet("SetNicoVideoId")]
    [Authorize(Policy = "DefaultAuthorization")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> SetNicoVideoId([FromQuery, Required] Guid itemId, [FromQuery, Required] string videoId)
    {
        var item = _libraryManager.GetItemById(itemId);
        if (item == null)
        {
            return BadRequest("Invalid item id.");
        }

        await ManifestManager.SetVideoId(item, videoId);

        return Ok();
    }

    [HttpGet("GetNicoVideoId")]
    [Authorize(Policy = "DefaultAuthorization")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> GetNicoVideoId([FromQuery, Required] Guid itemId)
    {
        var item = _libraryManager.GetItemById(itemId);
        if (item == null)
        {
            return BadRequest("Invalid item id.");
        }
        var manifest = await ManifestManager.GetManifest(item);

        if (manifest.VideoId == null)
        {
            return NotFound("Specified itemId is not linked to any videoId.");
        }

        var videoId = manifest.VideoId;

        return Ok(videoId);
    }

    /// <summary>
    /// Get comments from item id.
    /// </summary>
    /// <param name="itemId">item id.</param>
    /// <response code="200">Success.</response>
    /// <response code="404">This item is not linked to any nico link.</response>
    /// <response code="500">Some error.</response>
    /// <returns>Associated BIF file, or a <see cref="NotFoundResult"/>.</returns>
    [HttpGet("FetchComments/{itemId}")]
    [Authorize(Policy = "DefaultAuthorization")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> FetchComments([FromRoute, Required] Guid itemId)
    {
        var item = _libraryManager.GetItemById(itemId);
        if (item == null)
        {
            _logger.LogError($"[FlowComment/FetchComments] Item not found ({itemId})");
            return BadRequest("Invalid item id.");
        }
        var manifest = await ManifestManager.GetManifest(item).ConfigureAwait(false);

        if (manifest.VideoId == null)
        {
            _logger.LogInformation($"[FlowComment/FetchComments] VideoId id not found for {itemId}");
            return NotFound("Specified itemId is not linked to any videoId.");
        }

        var videoId = manifest.VideoId;

        using (var client = new HttpClient())
        {
            _logger.LogInformation($"[FlowComment/FetchComments] Fetching info ({itemId})");
            // fetch video info
            var videoInfoReq = new HttpRequestMessage();
            if (string.IsNullOrEmpty(_config.NiconicoSession))
            {
                _logger.LogDebug("[FlowComment/FetchComments] Session id not found.");
                videoInfoReq = new HttpRequestMessage(
                    HttpMethod.Get,
                    $"https://www.nicovideo.jp/api/watch/v3_guest/{videoId}?actionTrackId=1g9hKPLpnU_1624006273");
                videoInfoReq.Headers.Add("User-Agent", "Niconico/1.0 (Linux; U; Android 11; ja-jp; nicoandroid GR1YH) Version/7.11.0");
                videoInfoReq.Headers.Add("x-frontend-id", "1");
                videoInfoReq.Headers.Add("x-frontend-version", "7.11.0");
            }
            else
            {
                _logger.LogDebug($"[FlowComment/FetchComments] Session id found.");
                videoInfoReq = new HttpRequestMessage(
                    HttpMethod.Get,
                    $"https://www.nicovideo.jp/api/watch/v3/{videoId}?actionTrackId=1g9hKPLpnU_1624006275");
                videoInfoReq.Headers.Add("User-Agent", "Niconico/1.0 (Linux; U; Android 11; ja-jp; nicoandroid GR1YH) Version/7.11.0");
                videoInfoReq.Headers.Add("x-frontend-id", "1");
                videoInfoReq.Headers.Add("x-frontend-version", "7.11.0");
                videoInfoReq.Headers.Add("cookie", $"user_session={_config.NiconicoSession}");
            }

            JsonNode commentData;

            try
            {
                var videoInfoRes = await client.SendAsync(videoInfoReq).ConfigureAwait(false);
                JsonNode? videoInfo = JsonNode.Parse(await videoInfoRes.Content.ReadAsStringAsync().ConfigureAwait(false));

                var nvComment = videoInfo?["data"]?["comment"]?["nvComment"];

                if (!videoInfoRes.IsSuccessStatusCode || videoInfo == null || nvComment == null)
                {
                    String msg;
                    if (videoInfo == null)
                    {
                        msg = "Video info fetch error.";
                    }
                    else if (nvComment == null)
                    {
                        msg = "Comments not found.";
                    }
                    else
                    {
                        msg = "Invalid status code: " + videoInfoRes.StatusCode;
                    }

                    _logger.LogError("[FlowComment/FetchComments]" + msg);
                    var errorJson = new JsonObject();
                    _logger.LogInformation(videoInfo?.ToJsonString());
                    var metaJsonStr = videoInfo?["meta"]?.ToJsonString();
                    if (metaJsonStr != null)
                    {
                        errorJson["meta"] = JsonNode.Parse(metaJsonStr);
                    }
                    errorJson["message"] = msg;
                    var reasonCode = (string?)videoInfo?["data"]?["reasonCode"];
                    if (reasonCode != null)
                    {
                        errorJson["reasonCode"] = reasonCode;
                    }
                    _logger.LogError("[FlowComment/FetchComments] " + errorJson.ToJsonString());
                    throw new Exception();
                }
                // fetch comment
                var commentUrl = (string)(nvComment["server"]!) + "/v1/threads";

                var commentReq = new HttpRequestMessage(HttpMethod.Post, commentUrl);
                commentReq.Headers.Add("User-Agent", "Niconico/1.0 (Linux; U; Android 11; ja-jp; nicoandroid ASUS_I01WD) Version/7.11.0");
                commentReq.Headers.Add("x-frontend-id", "1");
                commentReq.Headers.Add("x-frontend-version", "7.11.0");
                var commentReqBody = new JsonObject();
                commentReqBody["params"] = JsonNode.Parse(nvComment["params"]!.ToJsonString());
                commentReqBody["additionals"] = null;
                commentReqBody["threadKey"] = (string)nvComment["threadKey"]!;
                commentReq.Content = new StringContent(commentReqBody.ToString(), Encoding.UTF8, "application/json");

                var commentRes = await client.SendAsync(commentReq).ConfigureAwait(false);
                var commentDataStr = await commentRes.Content.ReadAsStringAsync().ConfigureAwait(false);
                var comments = JsonNode.Parse(commentDataStr)!;

                if (!commentRes.IsSuccessStatusCode || comments == null)
                {
                    _logger.LogError("[FlowComment/FetchComments] Comment fetch error.");
                    var errorJson = new JsonObject();
                    errorJson["meta"] = JsonNode.Parse(nvComment["meta"]!.ToJsonString());
                    errorJson["message"] = "Failed to fetch comments";
                    _logger.LogError("[FlowComment/FetchComments] " + errorJson.ToJsonString());
                    throw new Exception();
                }

                commentData = comments;
                await ManifestManager.SetCommentData(item, comments.ToJsonString()).ConfigureAwait(false);
            }
            catch
            {
                _logger.LogWarning($"[FlowComment/FetchComments] Failed to fetch data of {videoId} from niconico. Falling back to local cache.");
                if (manifest.CommentData != null)
                {
                    var comments = JsonNode.Parse(manifest.CommentData);
                    if (comments == null)
                    {
                        _logger.LogError("[FlowComment/FetchComments] Comment data parse error from manifest.");
                        var errorJson = new JsonObject();
                        errorJson["message"] = "Comment data parse error from manifest";
                        return StatusCode(500, errorJson);
                    }
                    commentData = comments;
                }
                else
                {
                    _logger.LogError("[FlowComment/FetchComments] Video info fetch error.");
                    var errorJson = new JsonObject();
                    errorJson["meta"] = null;
                    errorJson["message"] = "Failed to fetch video info";
                    errorJson["reasonCode"] = null;
                    return StatusCode(500, errorJson);
                }
            }




            var returnData = new JsonObject();
            returnData["data"] = JsonNode.Parse(commentData["data"]!["threads"]!.ToJsonString());
            returnData["videoId"] = videoId;

            return Ok(returnData);
        }
    }
}
