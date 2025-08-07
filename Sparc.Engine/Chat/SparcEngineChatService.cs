using Sparc.Blossom.Authentication;
using Sparc.Blossom.Data;
using Sparc.Core.Chat;
using System.Security.Claims;

namespace Sparc.Engine.Chat;

public class SparcEngineChatService(
    IRepository<Room> rooms,
    IRepository<RoomMembership> memberships,
    IRepository<MatrixMessage> messageEvents,
    IRepository<BlossomUser> users,
    IHttpContextAccessor httpContextAccessor
) : IBlossomEndpoints
{
    IRepository<Room> Rooms = rooms;
    IRepository<RoomMembership> Memberships = memberships;
    IRepository<MatrixMessage> MessageEvents = messageEvents;
    IRepository<BlossomUser> Users = users;
    IHttpContextAccessor HttpContextAccessor = httpContextAccessor;

    public SparcAuthenticator<BlossomUser> Auth { get; } = auth;

    private async Task<string> GetMatrixUserAsync()
    {
        var principal = HttpContextAccessor.HttpContext?.User
            ?? throw new InvalidOperationException("User not authenticated");

        var user = await Auth.GetAsync(principal);

        // Ensure the user has a Matrix identity
        var username = user.Avatar.Username.ToLowerInvariant();
        var matrixId = $"@{username}:engine.sparc.coop";

        if (!user.HasIdentity("Matrix"))
        {
            user.AddIdentity("Matrix", matrixId);
            await Auth.UpdateAsync(user);
        }

        return user.Identity("Matrix")!;
    }

    private async Task<BlossomAvatar> GetUserAsync(ClaimsPrincipal principal)
    {
        if (principal == null)
            throw new ArgumentNullException(nameof(principal));
        var user = await Auth.GetAsync(principal);
        return user.Avatar;
    }

    private async Task<MatrixPresence> GetPresenceAsync(ClaimsPrincipal principal, string userId)
    {
        var user = await Auth.GetAsync(principal);
        return new MatrixPresence(user.Avatar);
    }

    private async Task SetPresenceAsync(ClaimsPrincipal principal, string userId, MatrixPresence presence)
    {
        var user = await Auth.GetAsync(principal);
        presence.ApplyToAvatar(user.Avatar);
        user.UpdateAvatar(user.Avatar);
        await Auth.UpdateAsync(user);
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

    private async Task<Room> CreateRoomAsync(CreateRoomRequest request)
    {
        var matrixId = await GetMatrixUserAsync();
        var room = new Room(request.Name)
        {
            CreatorUserId = matrixId,
            IsPrivate = request.Visibility == "private"
        };
        await Rooms.AddAsync(room);

        var membership = new RoomMembership
        {
            RoomId = room.RoomId,
            UserId = matrixId,
            Membership = "join",
            AssignedAt = DateTimeOffset.UtcNow
        };

        room.Memberships.Add(membership);
        await Memberships.AddAsync(membership);
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

        await Memberships.AddAsync(membership);
    }

    private async Task LeaveRoomAsync(string roomId)
    {
        var matrixId = await GetMatrixUserAsync();
        var membership = await Memberships.Query
            .Where(m => m.RoomId == roomId && m.UserId == matrixId)
            .FirstOrDefaultAsync();

        if (membership != null)
            await Memberships.DeleteAsync(membership);
    }

    private Task InviteToRoomAsync(string roomId, InviteToRoomRequest request)
    {
        throw new NotImplementedException();
    }

    private async Task<MatrixEvent> SendMessageAsync(string roomId, string eventType, string txnId, SendMessageRequest request)
    {
        var matrixId = await GetMatrixUserAsync();

        var message = new MatrixMessage()
        {
            Body = request.Body,
            MsgType = request.MsgType,
            Sender = matrixId,
            RoomId = roomId,
            Type = eventType,
            CreatedDate = DateTimeOffset.UtcNow
        };

        await MessageEvents.AddAsync(message);
        return message;
    }

    private async Task<List<MatrixMessage>> GetMessagesAsync(string roomId)
    {
        var messages = await MessageEvents.Query
            .Where(m => m.RoomId == roomId)
            .OrderByDescending(m => m.CreatedDate)
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