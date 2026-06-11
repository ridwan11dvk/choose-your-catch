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
    /// i.e. it's a valid catch for their current location and context (unless AllowAllFish is on).</summary>
    public static Func<string, bool>? IsFishAvailableForLocalPlayer { get; set; }

    /// <summary>Called when the local player's selected target fish isn't available at their current
    /// location, so normal fishing is used instead for this catch.</summary>
    public static Action<string>? OnLocalFishUnavailable { get; set; }

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

            var isLocalPlayer = player.UniqueMultiplayerID == Game1.player.UniqueMultiplayerID;

            if (isLocalPlayer && IsFishAvailableForLocalPlayer?.Invoke(selection.ItemId) == false)
            {
                OnLocalFishUnavailable?.Invoke(selection.DisplayName);
                return;
            }

            var quality = isLocalPlayer
                ? GetLocalDefaultQuality?.Invoke() ?? selection.Quality
                : selection.Quality;

            var replacement = ItemRegistry.Create(selection.ItemId);
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
