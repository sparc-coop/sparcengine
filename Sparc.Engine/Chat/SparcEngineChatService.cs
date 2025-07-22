using Sparc.Blossom.Authentication;
using Sparc.Core.Billing;
using Sparc.Core.Chat;
using Sparc.Engine.Billing.Stripe;
using System.Security.Claims;

namespace Sparc.Engine.Chat;

public class SparcEngineChatService(IRepository<Room> rooms) : IBlossomEndpoints
{

    IRepository<Room> Rooms = rooms;

    private async Task LeaveRoomAsync(HttpContext context)
    {
        throw new NotImplementedException();
    }

    private async Task JoinRoomAsync()
    {
        throw new NotImplementedException();
    }

    private async Task CreateRoomAsync(Room room)
    {
        //var newRoom = new Room(newRoomName); 
        
        await Rooms.AddAsync(room);

        return;
    }

    public void Map(IEndpointRouteBuilder endpoints)
    {
        var chatGroup = endpoints.MapGroup("/chat");

        chatGroup.MapPost("/createroom", CreateRoomAsync);

        //chatGroup.MapPost("/createroom", async (Room room) =>
        //{
        //    var roomResult = await CreateRoomAsync(room);
        //    return roomResult;
        //});
        chatGroup.MapPost("/rooms/{roomId}/join", JoinRoomAsync);
        chatGroup.MapPost("/rooms/{roomId}/leave", LeaveRoomAsync);
    }
}