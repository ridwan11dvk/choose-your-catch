using StardewModdingAPI;
using StardewValley;

namespace TargetFishSync;

internal static class TargetFishPatches
{
    public static TargetFishService? Service { get; set; }

    public static IMonitor? Monitor { get; set; }

    public static void GetFishFromLocationDataPostfix(Farmer player, ref Item __result)
    {
        TryReplaceFish(player, ref __result);
    }

    public static void StartMinigameEndFunctionPrefix(ref Item fish)
    {
        TryReplaceFish(Game1.player, ref fish);
    }

    private static void TryReplaceFish(Farmer? player, ref Item fish)
    {
        if (player is null)
        {
            return;
        }

        var selection = Service?.Get(player.UniqueMultiplayerID);
        if (selection is null)
        {
            return;
        }

        try
        {
            if (!ItemRegistry.Exists(selection.ItemId))
            {
                Monitor?.Log($"Target fish '{selection.ItemId}' no longer exists; using normal fishing.", LogLevel.Warn);
                Service?.Set(player.UniqueMultiplayerID, null);
                return;
            }

            var replacement = ItemRegistry.Create(selection.ItemId);
            ApplyQuality(replacement, selection.Quality);
            fish = replacement;
        }
        catch (Exception ex)
        {
            Monitor?.Log($"Couldn't create target fish '{selection.ItemId}': {ex.Message}", LogLevel.Warn);
        }
    }

    private static void ApplyQuality(Item item, FishQuality quality)
    {
        if (quality == FishQuality.Random)
        {
            return;
        }

        if (item is StardewValley.Object obj)
        {
            obj.Quality = (int)quality;
        }
    }
}
