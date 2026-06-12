using StardewModdingAPI;
using StardewValley;

namespace TargetFishSync;

internal static class TargetFishPatches
{
    public static TargetFishService? Service { get; set; }

    public static IMonitor? Monitor { get; set; }

    /// <summary>Returns the local player's current Default Quality config value, read live so config
    /// changes apply immediately without needing to reselect a target fish.</summary>
    public static Func<FishQuality>? GetLocalDefaultQuality { get; set; }

    /// <summary>Returns whether the given target fish can currently be caught by the local player,
    /// i.e. it's a valid catch for their current location and context (unless AllowAllFish is on).
    /// This is a safety net; ModEntry normally clears the selection on warp before this matters.</summary>
    public static Func<string, bool>? IsFishAvailableForLocalPlayer { get; set; }

    internal static Func<string, bool> ItemExists { get; set; } = ItemRegistry.Exists;

    internal static Func<string, Item> CreateItem { get; set; } = itemId => ItemRegistry.Create(itemId);

    public static void GetFishPostfix(Farmer who, ref Item __result)
    {
        TryReplaceFish(who, ref __result);
    }

    public static void GetFishFromLocationDataPostfix(Farmer player, ref Item __result)
    {
        TryReplaceFish(player, ref __result);
    }

    public static void StartMinigameEndFunctionPrefix(ref Item fish)
    {
        TryReplaceFish(Game1.player, ref fish);
    }

    public static void CreateFishPostfix(ref Item __result)
    {
        // CreateFish() builds the actual item the player receives from
        // FishingRod.whichFish/fishQuality, independent of the Item passed to
        // startMinigameEndFunction, so the chosen quality must be applied here.
        var selection = Service?.Get(Game1.player.UniqueMultiplayerID);
        if (selection is null || __result is null)
        {
            return;
        }

        // CreateFish always runs for the local player, so prefer the live config value over the
        // (possibly stale) snapshot taken when the fish was selected in the menu.
        var quality = GetLocalDefaultQuality?.Invoke() ?? selection.Quality;
        ApplyQuality(__result, quality);
    }

    private static void TryReplaceFish(Farmer? player, ref Item fish)
    {
        if (player is null)
        {
            return;
        }

        ReplaceSelectedCatch(
            player.UniqueMultiplayerID,
            player.UniqueMultiplayerID == Game1.player.UniqueMultiplayerID,
            ref fish);
    }

    internal static void ReplaceSelectedCatch(long playerId, bool isLocalPlayer, ref Item fish)
    {
        var selection = Service?.Get(playerId);
        if (selection is null)
        {
            return;
        }

        try
        {
            if (!ItemExists(selection.ItemId))
            {
                Monitor?.Log($"Target fish '{selection.ItemId}' no longer exists; using normal fishing.", LogLevel.Warn);
                Service?.Set(playerId, null);
                return;
            }

            if (isLocalPlayer && IsFishAvailableForLocalPlayer?.Invoke(selection.ItemId) == false)
            {
                return;
            }

            var quality = isLocalPlayer
                ? GetLocalDefaultQuality?.Invoke() ?? selection.Quality
                : selection.Quality;

            var replacement = CreateItem(selection.ItemId);
            ApplyQuality(replacement, quality);
            fish = replacement;
        }
        catch (Exception ex)
        {
            Monitor?.Log($"Couldn't create target fish '{selection.ItemId}': {ex.Message}", LogLevel.Warn);
        }
    }

    private static void ApplyQuality(Item item, FishQuality quality)
    {
        if (quality == FishQuality.Vanilla)
        {
            return;
        }

        if (item is StardewValley.Object obj)
        {
            obj.Quality = (int)quality;
        }
    }
}
