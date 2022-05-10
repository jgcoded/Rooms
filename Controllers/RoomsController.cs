using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Lib.AspNetCore.ServerSentEvents;
using p2p_api.Extensions;
using p2p_api.Services;

namespace p2p_api.Controllers;

[ApiController]
[Route("rooms")]
public class RoomsController : ControllerBase
{

    private RoomsService roomsService;
    private readonly IServerSentEventsService sseService;
    public RoomsController(RoomsService roomsService, IServerSentEventsService sseService)
    {
        this.roomsService = roomsService;
        this.sseService = sseService;
    }

    [Authorize("UserInRoom")]
    [Route("{roomName:required}")]
    [HttpPost]
    public ActionResult Post(string roomName, object data)
    {
        this.sseService.SendEventAsync(roomName, data.ToString());
        return Ok();
    }
}