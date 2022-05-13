using System.Collections.Concurrent;
using Lib.AspNetCore.ServerSentEvents;

using p2p_api.Extensions;
using p2p_api.Models;

namespace p2p_api.Services;

public class RoomService
{
    public const string RoomOwner = "RoomOwner";

    public const string RoomGuest = "RoomGuest";

    // TODO consider in-memory sqlite db
    private ConcurrentDictionary<string, bool> roomUserSet;

    private ConcurrentDictionary<Guid, string> roomIdToOwner;

    public RoomService()
    {
        this.roomUserSet = new ConcurrentDictionary<string, bool>(
            Environment.ProcessorCount,
            capacity: 100 // preallocate 100 users keys
        );

        this.roomIdToOwner =  new ConcurrentDictionary<Guid, string>(
            Environment.ProcessorCount,
            capacity: 100 // preallocate 100 rooms
        );
    }

    public static Action<IServerSentEventsService, ServerSentEventsClientConnectedArgs> OnClientConnected { get; } =
        (service, args) =>
        {
            string? roomId = args.Request.RouteValues[nameof(RoomClaims.RoomId)] as string;
            string userId = args.Client.User.UserId();

            if (string.IsNullOrWhiteSpace(roomId) || string.IsNullOrWhiteSpace(userId))
            {
                args.Client.Disconnect();
                return;
            }

            var roomsService = args.Request.HttpContext.RequestServices.GetRequiredService<RoomService>();
            roomsService.AddUserToRoom(Guid.Parse(roomId), userId);
            service.AddToGroup(roomId, args.Client);
        };

    public static Action<IServerSentEventsService, ServerSentEventsClientDisconnectedArgs> OnClientDisconnected { get; } =
        (service, args) =>
        {
            string? roomId = args.Request.RouteValues[nameof(RoomClaims.RoomId)] as string;

            if (string.IsNullOrWhiteSpace(roomId))
            {
                return;
            }

            var roomsService = args.Request.HttpContext.RequestServices.GetRequiredService<RoomService>();
            roomsService.RemoveUserFromRoom(Guid.Parse(roomId), args.Client.User.UserId());
        };

    public Guid ReserveRoom(string ownerUserId)
    {
        Guid roomId = Guid.NewGuid();
        while (!roomIdToOwner.TryAdd(roomId, ownerUserId))
        {
            roomId = Guid.NewGuid();
        }

        return roomId;
    }

    public string? GetRoomOwner(Guid roomId)
    {
        this.roomIdToOwner.TryGetValue(roomId, out string? ownerUserId);
        return ownerUserId;
    }

    public void AddUserToRoom(Guid roomId, string userId)
    {
        this.roomUserSet[CreateUserRoomKey(roomId, userId)] = true;
    }

    public bool IsUserInRoom(Guid roomId, string userId)
    {
        return this.roomUserSet.TryGetValue(CreateUserRoomKey(roomId, userId), out _);
    }

    public void RemoveUserFromRoom(Guid roomId, string userId)
    {
        this.roomUserSet.TryRemove(CreateUserRoomKey(roomId, userId), out _);
        if (this.roomIdToOwner.TryGetValue(roomId, out string? ownerId))
        {
            if (userId == ownerId)
            {
                // who is the new room owner?
                // Could make a "promote guest" API that owner client
                // invokes when the room owner leaves.
                this.roomIdToOwner.TryRemove(roomId, out _);
            }
        }
    }

    private string CreateUserRoomKey(Guid roomId, string userId)
    {
        return $"{roomId}{userId.ToLower()}";
    }
}
