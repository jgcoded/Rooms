using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Lib.AspNetCore.ServerSentEvents;

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using p2p_api.Extensions;
using p2p_api.Models;
using p2p_api.Services;

namespace p2p_api.Controllers;

[ApiController]
[Route("rooms")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class RoomsController : ControllerBase
{
    private RoomsService roomsService;
    private readonly IServerSentEventsService sseService;
    public RoomsController(RoomsService roomsService, IServerSentEventsService sseService)
    {
        this.roomsService = roomsService;
        this.sseService = sseService;
    }

    [Route("{roomName:required}/message")]
    [HttpPost]
    public ActionResult PostMessage(string roomName, object data)
    {
        // User can only send to rooms they have joined
        if (!this.roomsService.IsUserInRoom(roomName, User.UserId()))
        {
            return Unauthorized();
        }

        this.sseService.SendEventAsync(roomName, data.ToString());

        return Ok();
    }

    [Route("{roomName:required}/token")]
    [HttpGet]
    public async Task<ActionResult> GetRoomToken(string roomName)
    {
        int tokenExpirySeconds = 300;
        var expiry = DateTime.UtcNow.AddSeconds(tokenExpirySeconds);
        var roomToken = new RoomToken
        {
            RoomName = roomName,
            UserId = User.UserId(),
            Expiry = expiry
        };

        string token = await roomToken.Encrypt();
        string roomUrl = new UriBuilder()
        {
            Scheme = Request.Scheme,
            Host = Request.Host.Host,
            Port = Request.Host.Port ?? 443,
            Path = $"rooms/{roomName}",
            Query = $"t={System.Web.HttpUtility.UrlEncode(token)}"
        }.ToString();

        return Ok(new
        {
            roomUrl,
            token,
            expiry,
        });
    }
}