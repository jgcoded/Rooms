using System.Collections.Concurrent;
using Lib.AspNetCore.ServerSentEvents;

using p2p_api.Extensions;

namespace p2p_api.Services;

public class RoomsService
{
    private ConcurrentDictionary<string, bool> roomUserSet;

    public RoomsService()
    {
        this.roomUserSet = new ConcurrentDictionary<string, bool>(
            Environment.ProcessorCount,
            100 // preallocate 100 roomUser keys
        );
    }

    public void AddUserToRoom(string roomName, string userId)
    {
        this.roomUserSet[CreateUserRoomKey(roomName, userId)] = true;
    }

    public bool IsUserInRoom(string roomName, string userId)
    {
        return this.roomUserSet.TryGetValue(CreateUserRoomKey(roomName, userId), out bool _);
    }

    public void RemoveUserFromRoom(string roomName, string userId)
    {
        this.roomUserSet.TryRemove(CreateUserRoomKey(roomName, userId), out bool _);
    }

    private string CreateUserRoomKey(string roomName, string userId)
    {
        return $"{roomName.ToLower()}{userId.ToLower()}";
    }

    public static Action<IServerSentEventsService, ServerSentEventsClientConnectedArgs> OnClientConnected { get; } =
        (service, args) =>
        {
            string? roomName = args.Request.RouteValues["roomName"] as string;
            string userId = args.Client.User.UserId();

            if (roomName is null || string.IsNullOrWhiteSpace(userId))
            {
                args.Client.Disconnect();
                return;
            }

            var roomsService = args.Request.HttpContext.RequestServices.GetRequiredService<RoomsService>();
            roomsService.AddUserToRoom(roomName, userId);
            service.AddToGroup(roomName, args.Client);
        };

    public static Action<IServerSentEventsService, ServerSentEventsClientDisconnectedArgs> OnClientDisconnected { get; } =
        (service, args) =>
        {
            string? roomName = args.Request.RouteValues["roomName"] as string;

            if (roomName is null)
            {
                return;
            }

            var roomsService = args.Request.HttpContext.RequestServices.GetRequiredService<RoomsService>();
            roomsService.RemoveUserFromRoom(roomName, args.Client.User.UserId());
        };
}
