using Microsoft.Azure.Cosmos.Linq;
using System.Collections;
using System.ComponentModel.DataAnnotations;

namespace Timbii.Messages;
public class Room : BlossomEntity<string>
{
    [Required(ErrorMessage = "Creator is required")]
    public User Creator { get; set; }
    [Required(ErrorMessage = "RoomId is required")]
    public User Admin { get; set; }

    public List<User> Members { get; set; }
    public string Name { get; set; }

    public DateTime DateCreated { get; set; }
    public DateTime DateModified { get; set; }

    public Room(User creator, List<User> members) : base(Guid.NewGuid().ToString())
    {
        Creator = creator;
        Admin = creator;
        Members = members;
        Name = CreateRoomName(members);
        DateCreated = DateTime.UtcNow;
        DateModified = DateTime.UtcNow;
    }

    public string CreateRoomName(List<User> members)
    {
        //var memberList = members.ToList();
        Console.WriteLine(members.Count());

        if (members.Count == 1)
        {
            return members[0].FirstName;
        }
        else if (members.Count > 2)
        {
            return string.Join(", ", members.Take(2).Select(m => m.FirstName)) + " and others";
        }
        else
        {
            throw new ArgumentException("At least one member is required to create a room name", nameof(members));
        }
    }

    public void UpdateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Room name cannot be empty", nameof(name));
        }
        Name = name;
        DateModified = DateTime.UtcNow;
    }

    public void AddMember(User member)
    {
        if (member == null)
        {
            throw new ArgumentNullException(nameof(member), "Member cannot be null");
        }
        if (Members.Any(m => m.Id == member.Id))
        {
            throw new InvalidOperationException("Member already exists in the room");
        }

        Members.Add(member);
        DateModified = DateTime.UtcNow;
    }
}