using Sparc.Blossom.Authentication;
using Sparc.Blossom.Data;
using Sparc.Core.Chat;
using System.Security.Claims;

namespace Sparc.Engine.Chat;

public class SparcEngineChatService(
    IRepository<Room> rooms,
    IRepository<RoomMembership> memberships,
    IRepository<MatrixMessageEvent> events,
    SparcAuthenticator<BlossomUser> auth,
    IHttpContextAccessor httpContextAccessor
) : IBlossomEndpoints
{
    private async Task<string> GetMatrixUserAsync()
    {
        var principal = httpContextAccessor.HttpContext?.User
            ?? throw new InvalidOperationException("User not authenticated");

        var user = await auth.GetAsync(principal);

        // Ensure the user has a Matrix identity
        var username = user.Avatar.Username.ToLowerInvariant();
        var matrixId = $"@{username}:engine.sparc.coop";

        if (!user.HasIdentity("Matrix"))
        {
            user.AddIdentity("Matrix", matrixId);
            await auth.UpdateAsync(user);
        }

        return user.Identity("Matrix")!;
    }

    private async Task<BlossomAvatar> GetUserAsync(ClaimsPrincipal principal)
    {
        if (principal == null)
            throw new ArgumentNullException(nameof(principal));
        var user = await auth.GetAsync(principal);
        return user.Avatar;
    }

    private async Task<MatrixPresence> GetPresenceAsync(ClaimsPrincipal principal, string userId)
    {
        var user = await auth.GetAsync(principal);
        return new MatrixPresence(user.Avatar);
    }

    private async Task SetPresenceAsync(ClaimsPrincipal principal, string userId, MatrixPresence presence)
    {
        var user = await auth.GetAsync(principal);
        presence.ApplyToAvatar(user.Avatar);
        user.UpdateAvatar(user.Avatar);
        await auth.UpdateAsync(user);
    }

    private async Task<List<Room>> GetRoomsAsync()
    {
        var allRooms = await rooms.Query.ToListAsync();
        foreach (var room in allRooms)
        {
            var allMemberships = await memberships.Query.Where(x => x.RoomId == room.RoomId).ToListAsync();
            room.Memberships = allMemberships;
        }

        return allRooms;
    }

    private async Task<Room> CreateRoomAsync(CreateRoomRequest request)
    {
        var matrixId = await GetMatrixUserAsync();
        var room = new Room(request.Name)
        {
            CreatorUserId = matrixId,
            IsPrivate = request.Visibility == "private"
        };
        await rooms.AddAsync(room);

        var membership = new RoomMembership
        {
            RoomId = room.RoomId,
            UserId = matrixId,
            Membership = "join",
            AssignedAt = DateTimeOffset.UtcNow
        };

        room.Memberships.Add(membership);
        await memberships.AddAsync(membership);
        return room;
    }

    private async Task JoinRoomAsync(string roomId)
    {
        var matrixId = await GetMatrixUserAsync();
        var membership = new RoomMembership
        {
            RoomId = roomId,
            UserId = matrixId,
            Membership = "join",
            AssignedAt = DateTimeOffset.UtcNow
        };

        await memberships.AddAsync(membership);
    }

    private async Task LeaveRoomAsync(string roomId)
    {
        var matrixId = await GetMatrixUserAsync();
        var membership = await memberships.Query
            .Where(m => m.RoomId == roomId && m.UserId == matrixId)
            .FirstOrDefaultAsync();

        if (membership != null)
            await memberships.DeleteAsync(membership);
    }

    private Task InviteToRoomAsync(string roomId, InviteToRoomRequest request)
    {
        throw new NotImplementedException();
    }

    private async Task<MatrixMessageEvent> SendMessageAsync(string roomId, string eventType, string txnId, SendMessageRequest request)
    {
        var matrixId = await GetMatrixUserAsync();

        var message = new MatrixMessageEvent(roomId, matrixId, new MatrixMessage(request.Body));
        await events.AddAsync(message);

        return message;
    }

    private async Task<List<MatrixMessageEvent>> GetMessagesAsync(string roomId)
    {
        var messages = await events.Query
            .Where(m => m.RoomId == roomId && m.Type == "m.room.message")
            .OrderBy(m => m.Depth)
            .ToListAsync();

        return messages;
    }

    public void Map(IEndpointRouteBuilder endpoints)
    {
        var chatGroup = endpoints.MapGroup("/_matrix/client/v3");

        chatGroup.MapGet("/publicRooms", GetRoomsAsync);
        chatGroup.MapPost("/createRoom", CreateRoomAsync);
        chatGroup.MapPost("/join/{roomId}", JoinRoomAsync);
        chatGroup.MapPost("/rooms/{roomId}/leave", LeaveRoomAsync);
        chatGroup.MapPost("/rooms/{roomId}/invite", InviteToRoomAsync);
        chatGroup.MapGet("/rooms/{roomId}/messages", GetMessagesAsync);
        chatGroup.MapPost("/rooms/{roomId}/send/{eventType}/{txnId}", SendMessageAsync);
        chatGroup.MapGet("/matrixUser", GetMatrixUserAsync);
        chatGroup.MapGet("/user", GetUserAsync);

        // Map the presence endpoint
        chatGroup.MapGet("/presence/{userId}/status", async (ClaimsPrincipal principal, string userId) =>
        {
            return await GetPresenceAsync(principal, userId);
        });
        chatGroup.MapPut("/presence/{userId}/status", async (ClaimsPrincipal principal, string userId, MatrixPresence presence) =>
        {
            await SetPresenceAsync(principal, userId, presence);
            return Results.Ok();
        });
    }
}