using Microsoft.Azure.Cosmos.Linq;
using System.ComponentModel.DataAnnotations;

namespace Timbii.Messages;
public class Message : BlossomEntity<string>
{
    [Required(ErrorMessage = "Sender is required")]
    public User Sender { get; set; }
    [Required(ErrorMessage = "RoomId is required")]

    public string RoomId { get; set; }
    [Required(ErrorMessage = "Text is required")]

    public string Text { get; set; }
    public DateTime Timestamp { get; set; }

    public Message(User sender, string roomId, string text, DateTime timestamp) : base(Guid.NewGuid().ToString())
    {
        Sender = sender;
        RoomId = roomId;
        Text = text;
        Timestamp = (timestamp != DateTime.UtcNow ? timestamp : DateTime.UtcNow);
    }
}