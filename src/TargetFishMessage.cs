namespace TargetFishSync;

internal sealed class TargetFishMessage
{
    public long PlayerId { get; set; }

    public string? ItemId { get; set; }

    public string? DisplayName { get; set; }

    public FishQuality Quality { get; set; } = FishQuality.Random;

    public TargetFishSelection? ToSelection()
    {
        return string.IsNullOrWhiteSpace(ItemId)
            ? null
            : new TargetFishSelection(ItemId, DisplayName ?? ItemId, Quality);
    }

    public static TargetFishMessage From(long playerId, TargetFishSelection? selection)
    {
        return new TargetFishMessage
        {
            PlayerId = playerId,
            ItemId = selection?.ItemId,
            DisplayName = selection?.DisplayName,
            Quality = selection?.Quality ?? FishQuality.Random
        };
    }
}
