using Microsoft.EntityFrameworkCore;
using Sparc.Blossom.Authentication;
using Sparc.Blossom.Data;
using Sparc.Core.Billing;
using Sparc.Core.Chat;
using Sparc.Engine.Billing.Stripe;
using System.Net;
using System.Security.Claims;
using System.Text.Json;

namespace Sparc.Engine.Chat;

public class SparcEngineChatService(
    IRepository<Room> rooms,
    IRepository<RoomMembership> memberships,
    IRepository<MessageEvent> messageEvents,
    IRepository<BlossomUser> users,
    IHttpClientFactory httpClientFactory,
    IHttpContextAccessor httpContextAccessor
) : IBlossomEndpoints
{
    IRepository<Room> Rooms = rooms;
    IRepository<RoomMembership> Memberships = memberships;
    IRepository<MessageEvent> MessageEvents = messageEvents;
    IRepository<BlossomUser> Users = users;
    IHttpContextAccessor HttpContextAccessor = httpContextAccessor;
    HttpClient MatrixClient = httpClientFactory.CreateClient("Matrix");

    // Check the values below to match Matrix server configuration
    private string MatrixDomain = "https://localhost:7185";
    private string MatrixPassword = "password";

    private async Task<BlossomUser> GetUserAsync()
    {
        var principal = HttpContextAccessor.HttpContext?.User
            ?? throw new InvalidOperationException("User not authenticated");

        var id = principal.Claims.FirstOrDefault(c => c.Type == "externalId")?.Value;

        var user = await Users.Query.FirstOrDefaultAsync(u =>
            u.Identities.Any(i => i.Id == id)) ?? throw new Exception("User not found");

        return user;
    }

    private async Task EnsureMatrixUserAsync(BlossomUser user)
    {
        var username = user.Avatar.Username.ToLowerInvariant();
        var matrixId = $"@{username}:{MatrixDomain}";

        if (!user.HasIdentity("Matrix"))
            user.AddIdentity("Matrix", matrixId);

        var payload = new
        {
            username,
            password = MatrixPassword,
            auth = new { type = "m.login.dummy" }  // can we do this? 
        };

        var response = await MatrixClient.PostAsJsonAsync("/_matrix/client/v3/register", payload);

        if (!response.IsSuccessStatusCode && response.StatusCode != HttpStatusCode.Conflict)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Error creating Matrix user: {error}");
        }

        await Users.UpdateAsync(user);
    }

    private async Task<string> GetMatrixAccessTokenAsync(BlossomUser user)
    {
        var username = user.Avatar.Username.ToLowerInvariant();

        var payload = new
        {
            type = "m.login.password",
            user = username,
            password = MatrixPassword
        };

        var response = await MatrixClient.PostAsJsonAsync("/_matrix/client/v3/login", payload);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Error in Matrix authentication: {error}");
        }

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty("access_token").GetString();    // check refresh token handling
    }

    private async Task<Room> CreateRoomAsync(Room room)
    {
        var user = await GetUserAsync();
        await EnsureMatrixUserAsync(user);
        var token = await GetMatrixAccessTokenAsync(user);

        var response = await MatrixClient.PostAsJsonAsync(
            $"/_matrix/client/v3/createRoom?access_token={token}",
            new { name = room.RoomName, preset = "private_chat" }
        );

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Error creating room in Matrix: {error}");
        }

        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        var matrixRoomId = result.GetProperty("room_id").GetString();
        room.RoomId = matrixRoomId!;
        room.CreatorUserId = user.Id;

        var membership = new RoomMembership
        {
            RoomId = room.RoomId,
            UserId = user.Id,
            Membership = "join",
            AssignedAt = DateTimeOffset.UtcNow
        };

        room.Memberships.Add(membership);

        await Rooms.AddAsync(room);
        await Memberships.AddAsync(membership);
        return room;
    }

    private async Task<List<Room>> GetRoomsAsync(string userId)
    {
        var rooms = await Rooms.Query.ToListAsync();
        foreach (var room in rooms)
        {
            var memberships = await Memberships.Query.Where(x => x.RoomId == room.RoomId).ToListAsync();
            room.Memberships = memberships;
        }

        return rooms;
    }

    private async Task<Event> CreateMessageEventAsync(MessageEvent message)
    {
        var user = await GetUserAsync();
        await EnsureMatrixUserAsync(user);
        var token = await GetMatrixAccessTokenAsync(user);

        var payload = new
        {
            msgtype = message.MsgType ?? "m.text",
            body = message.Body
        };

        var txnId = Guid.NewGuid().ToString("N");

        var response = await MatrixClient.PutAsJsonAsync(
            $"/_matrix/client/v3/rooms/{message.RoomId}/send/m.room.message/{txnId}?access_token={token}",
            payload
        );

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Error sending message en Matrix: {error}");
        }

        message.Sender = user.Identity("Matrix")!;
        message.Type = "m.room.message";
        message.Content = message.Body;
        message.CreatedDate = DateTimeOffset.UtcNow;

        await MessageEvents.AddAsync(message);
        return message;
    }

    private async Task<List<MessageEvent>> GetMessagesAsync(string roomId)
    {
        var user = await GetUserAsync();
        var token = await GetMatrixAccessTokenAsync(user);

        var response = await MatrixClient.GetAsync(
            $"/_matrix/client/v3/rooms/{roomId}/messages?dir=b&access_token={token}"
        );

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Error getting messages: {error}");
        }

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var messages = new List<MessageEvent>();

        foreach (var item in json.GetProperty("chunk").EnumerateArray())
        {
            if (item.GetProperty("type").GetString() == "m.room.message")
            {
                var content = item.GetProperty("content");
                messages.Add(new MessageEvent
                {
                    RoomId = roomId,
                    MsgType = content.GetProperty("msgtype").GetString() ?? "m.text",
                    Body = content.GetProperty("body").GetString() ?? "",
                    Sender = item.GetProperty("sender").GetString() ?? "",
                });
            }
        }

        return messages;
    }

    private async Task JoinRoomAsync(string roomId)
    {
        var user = await GetUserAsync();
        await EnsureMatrixUserAsync(user);
        var token = await GetMatrixAccessTokenAsync(user);

        var response = await MatrixClient.PostAsJsonAsync(
            $"/_matrix/client/v3/rooms/{roomId}/join?access_token={token}", new { }
        );

        if (!response.IsSuccessStatusCode && response.StatusCode != HttpStatusCode.Forbidden)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Error joining room: {error}");
        }

        var membership = new RoomMembership
        {
            RoomId = roomId,
            UserId = user.Id,
            Membership = "join",
            AssignedAt = DateTimeOffset.UtcNow
        };

        await Memberships.AddAsync(membership);
    }

    private async Task LeaveRoomAsync(string roomId)
    {
        var user = await GetUserAsync();
        var token = await GetMatrixAccessTokenAsync(user);

        await MatrixClient.PostAsJsonAsync(
            $"/_matrix/client/v3/rooms/{roomId}/leave?access_token={token}", new { }
        );

        var membership = await Memberships.Query
            .FirstOrDefaultAsync(m => m.RoomId == roomId && m.UserId == user.Id);

        if (membership != null)
            await Memberships.DeleteAsync(membership);
    }

    public void Map(IEndpointRouteBuilder endpoints)
    {
        var chatGroup = endpoints.MapGroup("/chat");

        chatGroup.MapGet("/getrooms", GetRoomsAsync);
        chatGroup.MapPost("/rooms/create", CreateRoomAsync);
        chatGroup.MapPost("/rooms/{roomId}/join", JoinRoomAsync);
        chatGroup.MapPost("/rooms/{roomId}/leave", LeaveRoomAsync);

        chatGroup.MapGet("/rooms/{roomId}/messages", GetMessagesAsync);
        chatGroup.MapPost("/rooms/sendmessage", CreateMessageEventAsync);
    }
}