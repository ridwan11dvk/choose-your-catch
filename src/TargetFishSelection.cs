namespace TargetFishSync;

internal sealed class TargetFishSelection
{
    public TargetFishSelection(string itemId, string displayName, FishQuality quality)
    {
        ItemId = itemId;
        DisplayName = displayName;
        Quality = quality;
    }

    public string ItemId { get; }

    public string DisplayName { get; }

    public FishQuality Quality { get; }
}
