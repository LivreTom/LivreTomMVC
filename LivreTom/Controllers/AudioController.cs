using LivreTom.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Mime;
using System.Security.Claims;

namespace LivreTom.Controllers;

[Route("api/audio")]
[Authorize]
public class AudioController(MusicService musicService, HttpClient httpClient) : Controller
{
    [HttpGet("{orderId:int}")]
    public async Task<IActionResult> Stream(int orderId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var order = await musicService.GetOrderByIdAsync(orderId);
        if (order is null || order.UserId != userId)
            return NotFound();

        if (!order.DownloadConfirmed)
            return Forbid();

        var audioUrl = order.ResolvedAudioUrl;
        if (string.IsNullOrEmpty(audioUrl))
            return NotFound();

        var bytes = await httpClient.GetByteArrayAsync(audioUrl);

        var filename = $"{order.Title ?? "musica"}.mp3";
        var cd = new ContentDisposition
        {
            Inline = true,
            FileName = filename
        };
        Response.Headers.ContentDisposition = cd.ToString();
        Response.Headers.CacheControl = "private, max-age=86400";

        return File(bytes, "audio/mpeg", enableRangeProcessing: true);
    }

    [HttpGet("{orderId:int}/preview")]
    public async Task<IActionResult> Preview(int orderId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var order = await musicService.GetOrderByIdAsync(orderId);
        if (order is null || order.UserId != userId)
            return NotFound();

        var audioUrl = order.ResolvedAudioUrl;
        if (string.IsNullOrEmpty(audioUrl))
            return NotFound();

        var bytes = await httpClient.GetByteArrayAsync(audioUrl);
        Response.Headers.CacheControl = "private, max-age=86400";
        return File(bytes, "audio/mpeg", enableRangeProcessing: false);
    }
}
