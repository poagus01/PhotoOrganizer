using LiteDB;

namespace PhotoOrganizer.Core.Models;

public class MediaFileInfo
{
    [BsonId]
    public string Id { get; set; } = Guid.NewGuid().ToString();  // fixes warning about null Id

    public required string Path { get; set; }
    public required string Sha256Hash { get; set; }

    public ulong? VisualHash { get; set; }   // 🔥 added (fixes your first error)

    public DateTime? Taken { get; set; }
    public int? Year { get; set; }
}
