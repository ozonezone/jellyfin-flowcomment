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
            _logger.LogError($"[fetchcomments] Item not found ({itemId})");
            return BadRequest("Invalid item id.");
        }
        var manifest = await ManifestManager.GetManifest(item);

        if (manifest.VideoId == null)
        {
            _logger.LogInformation($"[fetchcomments] VideoId id not found for {itemId}");
            return NotFound("Specified itemId is not linked to any videoId.");
        }

        var videoId = manifest.VideoId;

        using (var client = new HttpClient())
        {
            _logger.LogInformation($"[fetchcomments] Fetching info ({itemId})");
            // fetch video info
            var videoInfoReq = new HttpRequestMessage();
            if (string.IsNullOrEmpty(_config.NiconicoSession))
            {
                _logger.LogDebug("[fetchcomments] Session id not found.");
                videoInfoReq = new HttpRequestMessage(
                    HttpMethod.Get,
                    $"https://www.nicovideo.jp/api/watch/v3_guest/{videoId}?actionTrackId=1g9hKPLpnU_1624006273");
                videoInfoReq.Headers.Add("User-Agent", "Niconico/1.0 (Linux; U; Android 11; ja-jp; nicoandroid GR1YH) Version/7.11.0");
                videoInfoReq.Headers.Add("x-frontend-id", "1");
                videoInfoReq.Headers.Add("x-frontend-version", "7.11.0");
            }
            else
            {
                _logger.LogDebug($"[fetchcomments] Session id found.");
                videoInfoReq = new HttpRequestMessage(
                    HttpMethod.Get,
                    $"https://www.nicovideo.jp/api/watch/v3/{videoId}?actionTrackId=1g9hKPLpnU_1624006275");
                videoInfoReq.Headers.Add("User-Agent", "Niconico/1.0 (Linux; U; Android 11; ja-jp; nicoandroid GR1YH) Version/7.11.0");
                videoInfoReq.Headers.Add("x-frontend-id", "1");
                videoInfoReq.Headers.Add("x-frontend-version", "7.11.0");
                videoInfoReq.Headers.Add("cookie", $"user_session={_config.NiconicoSession}");
            }

            var videoInfoRes = await client.SendAsync(videoInfoReq).ConfigureAwait(false);
            JsonNode videoInfo = JsonNode.Parse(await videoInfoRes.Content.ReadAsStringAsync().ConfigureAwait(false))!;

            if (!videoInfoRes.IsSuccessStatusCode)
            {
                _logger.LogError("[fetchcomments] Video info fetch error.");
                var errorJson = new JsonObject();
                _logger.LogInformation(videoInfo.ToJsonString());
                errorJson["meta"] = JsonNode.Parse(videoInfo["meta"]!.ToJsonString());
                errorJson["message"] = "Failed to fetch video info";
                errorJson["reasonCode"] = (string?)videoInfo["data"]?["reasonCode"];
                return StatusCode(500, errorJson);
            }

            // fetch comment
            var nvComment = videoInfo["data"]?["comment"]?["nvComment"]!;
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
            var commentData = JsonNode.Parse(commentDataStr)!;

            if (!commentRes.IsSuccessStatusCode)
            {
                _logger.LogError("[fetchcomments] Comment fetch error.");
                var errorJson = new JsonObject();
                errorJson["meta"] = JsonNode.Parse(commentData["meta"]!.ToJsonString());
                errorJson["message"] = "Failed to fetch comments";
                return StatusCode(500, errorJson);
            }


            return Ok(commentData["data"]!["threads"]);
        }
    }
}
