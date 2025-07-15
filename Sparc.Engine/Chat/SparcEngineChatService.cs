using Sparc.Blossom.Authentication;
using Sparc.Core.Billing;
using Sparc.Core.Chat;
using Sparc.Engine.Billing.Stripe;
using System.Security.Claims;

namespace Sparc.Engine.Chat;

public class SparcEngineChatService() : IBlossomEndpoints
{
    private async Task LeaveRoomAsync(HttpContext context)
    {
        throw new NotImplementedException();
    }

    private async Task JoinRoomAsync()
    {
        throw new NotImplementedException();
    }

    private async Task CreateRoomAsync()
    {
        throw new NotImplementedException();
    }

    public void Map(IEndpointRouteBuilder endpoints)
    {
        var chatGroup = endpoints.MapGroup("/chat");

        chatGroup.MapPost("/createroom", CreateRoomAsync);
        chatGroup.MapPost("/rooms/{roomId}/join", JoinRoomAsync);
        chatGroup.MapPost("/rooms/{roomId}/leave", LeaveRoomAsync);
    }
}