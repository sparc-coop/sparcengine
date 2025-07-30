using Refit;
using Sparc.Blossom.Authentication;
using Sparc.Core.Chat;

namespace Sparc.Engine.Chat;

public interface ISparcChat
{
    [Get("/chat/getrooms")]
    Task<List<Room>> GetRoomsAsync();

    [Post("/chat/rooms/create")]
    Task<Room> CreateRoomAsync([Body] Room room);

    [Post("/chat/rooms/{roomId}/join")]
    Task JoinRoomAsync([AliasAs("roomId")] string roomId);

    [Post("/chat/rooms/{roomId}/leave")]
    Task LeaveRoomAsync([AliasAs("roomId")] string roomId);

    [Get("/chat/rooms/{roomId}/messages")]
    Task<List<MessageEvent>> GetMessagesAsync([AliasAs("roomId")] string roomId);

    [Post("/chat/rooms/sendmessage")]
    Task<Event> SendMessageAsync([Body] MessageEvent message);
}
