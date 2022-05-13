using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Lib.AspNetCore.ServerSentEvents;

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using Rooms.Authorization;
using Rooms.Extensions;
using Rooms.Models;
using Rooms.Services;

namespace Rooms.Controllers;

[ApiController]
[Route("rooms")]
[Authorize]
public class RoomsController : ControllerBase
{
    private RoomService roomsService;
    private RoomTokenService roomsTokenService;
    private TurnCredentialsService turnCredentialsService;
    private readonly IServerSentEventsService sseService;

    public RoomsController(
        RoomService roomsService,
        RoomTokenService roomsTokenService,
        TurnCredentialsService turnCredentialsService,
        IServerSentEventsService sseService)
    {
        this.roomsService = roomsService;
        this.roomsTokenService = roomsTokenService;
        this.turnCredentialsService = turnCredentialsService;
        this.sseService = sseService;
    }

    [Authorize(UserInRoomRequirement.UserInRoom)]
    [Route("{roomId:guid:required}")]
    [HttpPost]
    public ActionResult PostMessage(Guid roomId, object data)
    {
        this.sseService.SendEventAsync(roomId.ToString(), data.ToString());
        return Ok();
    }

    [Route("")]
    [HttpPost]
    public ActionResult PostReserveRoom(Guid? roomId)
    {
        string userId = User.UserId();
        string country = User.Country();

        if (!string.Equals(country, "United States", StringComparison.InvariantCultureIgnoreCase))
        {
            throw new UnauthorizedAccessException();
        }

        string userRole = RoomService.RoomGuest;
        if (roomId.HasValue)
        {
            // user wants to join the room. Check if it exists.
            string? roomOwner = this.roomsService.GetRoomOwner(roomId.Value);
            if (string.IsNullOrWhiteSpace(roomOwner))
            {
                return BadRequest();
            }

            // Don't let the room owner join their own room
            if (userId == roomOwner)
            {
                return BadRequest();
            }
        }
        else // create the room
        {
            roomId = this.roomsService.ReserveRoom(userId);
            userRole = RoomService.RoomOwner;
        }

        var claims = new RoomClaims()
        {
            RoomId = roomId.Value,
            UserId = userId,
            UserRole = userRole
        };

        string jwt = this.roomsTokenService.CreateJwt(claims);

        var uriBuilder = new UriBuilder()
        {
            Scheme = Request.Scheme,
            Host = Request.Host.Host,
            Port = Request.Host.Port ?? 443,
            Path = $"rooms/{roomId}",
            Query = RoomTokenService.GetTokenQueryParameter(jwt).ToString()
        };

        string roomUrl = uriBuilder.ToString();

        uriBuilder.Query = null;
        string messageUrl = uriBuilder.ToString();

        TurnCredentials turnCredentials = this.turnCredentialsService.CreateTurnCredentials(userId);

        return Ok(new
        {
            roomId,
            roomUrl,
            messageUrl,
            turnCredentials
        });
    }
}