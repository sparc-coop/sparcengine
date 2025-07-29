using Microsoft.EntityFrameworkCore;
using Sparc.Blossom.Authentication;
using Sparc.Blossom.Data;
using Sparc.Core.Billing;
using Sparc.Core.Chat;
using Sparc.Engine.Billing.Stripe;
using System.Security.Claims;

namespace Sparc.Engine.Chat;

public class SparcEngineChatService(IRepository<Room> rooms, IRepository<RoomMembership> memberships, IRepository<MessageEvent> messageEvents) : IBlossomEndpoints
{

    IRepository<Room> Rooms = rooms;
    IRepository<RoomMembership> Memberships = memberships;
    IRepository<MessageEvent> MessageEvents = messageEvents;

    private async Task LeaveRoomAsync(HttpContext context)
    {
        throw new NotImplementedException();
    }

    private async Task JoinRoomAsync()
    {
        throw new NotImplementedException();
    }

    private async Task<Room> CreateRoomAsync(Room room)
    {
        var membership = new RoomMembership()
        {
            RoomId = room.RoomId,
            Membership = "join",
            AssignedAt = DateTimeOffset.UtcNow
        };

        room.Memberships.Add(membership);
        
        await Rooms.AddAsync(room);
        await Memberships.AddAsync(membership);
        return room;
    }

    private async Task<List<Room>> GetRoomsAsync()
    {
        var rooms = await Rooms.Query.ToListAsync();
        foreach (var room in rooms)
        {
            var memberships = await Memberships.Query.Where(x => x.RoomId == room.RoomId).ToListAsync();
            room.Memberships = memberships;
        }
        
        return rooms;
    }
    private async Task<List<MessageEvent>> GetMessagesAsync(string roomId)
    {

        var messages = await MessageEvents.Query
            .Where(e => e.RoomId == roomId)
            .ToListAsync();

        return messages;
    }

    private async Task<Event> CreateMessageEventAsync(MessageEvent message)
    {
        //var newMessage = new MessageEvent
        //{
        //    RoomId = roomId,
        //    Sender = "system", // Replace with actual sender logic
        //    Type = "message",
        //    Body = message,
        //    Content = message
        //};

        await MessageEvents.AddAsync(message);

        return message;
    }

    public void Map(IEndpointRouteBuilder endpoints)
    {
        var chatGroup = endpoints.MapGroup("/chat");

        // Rooms
        chatGroup.MapGet("/getrooms", GetRoomsAsync);
        chatGroup.MapPost("/rooms/create", CreateRoomAsync);
        chatGroup.MapPost("/rooms/{roomId}/join", JoinRoomAsync);
        chatGroup.MapPost("/rooms/{roomId}/leave", LeaveRoomAsync);

        //Events
        chatGroup.MapGet("/rooms/{roomId}/messages", GetMessagesAsync);
        chatGroup.MapPost("/rooms/sendmessage", CreateMessageEventAsync);
    }


}