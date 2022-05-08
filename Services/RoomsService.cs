using System.Collections.Concurrent;

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
}
