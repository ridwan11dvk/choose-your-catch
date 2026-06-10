using StardewValley;

namespace TargetFishSync;

internal sealed class FishEntry
{
    public FishEntry(
        string itemId,
        string displayName,
        Item previewItem,
        int price,
        bool canTrackCaughtStatus,
        bool isCaught)
    {
        ItemId = itemId;
        DisplayName = displayName;
        PreviewItem = previewItem;
        Price = Math.Max(0, price);
        CanTrackCaughtStatus = canTrackCaughtStatus;
        IsCaught = isCaught;
    }

    public string ItemId { get; }

    public string DisplayName { get; }

    public Item PreviewItem { get; }

    public int Price { get; }

    public bool CanTrackCaughtStatus { get; }

    public bool IsCaught { get; }
}
