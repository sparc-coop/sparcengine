using Refit;
using Sparc.Blossom.Authentication;
using Sparc.Core.Chat;

namespace Sparc.Engine.Chat;

public interface ISparcChat
{
    [Post("/chat/createroom")]
    Task<Room> CreateRoomAsync(Room room);
    [Post("/chat/rooms/{roomId}/join")]
    Task<Room> JoinRoomAsync(string roomId);
    [Post("/chat/rooms/{roomId}/leave")]
    Task<Room> LeaveRoomAsync(string roomId);
    [Post("/chat/rooms/{roomId}/invite")]
    Task<Room> InviteRoomAsync(string roomId, string userId);
}
