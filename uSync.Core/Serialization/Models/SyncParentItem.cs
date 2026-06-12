namespace uSync.Core.Serialization.Models;

public record SyncParentItem {
    public required int Id { get; init; }
    public required Guid Key { get; init; }
    public required string Name { get; init; }
    public string? Path { get; set; }
    public int Level { get; set; }
}
