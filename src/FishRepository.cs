using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.GameData.Locations;
using StardewValley.Locations;
using StardewValley.Tools;

namespace TargetFishSync;

internal sealed class FishRepository
{
    private readonly IMonitor Monitor;
    private readonly Func<ModConfig> GetConfig;
    private List<string>? AllFishIdCache;

    public FishRepository(IMonitor monitor, Func<ModConfig> getConfig)
    {
        Monitor = monitor;
        GetConfig = getConfig;
    }

    public void ClearCache()
    {
        AllFishIdCache = null;
    }

    public List<FishEntry> GetFishForCurrentContext(bool allowAllFish)
    {
        var config = GetConfig();
        var player = Game1.player;
        if (player is null)
        {
            return new List<FishEntry>();
        }

        var allFish = allowAllFish
            ? GetAllKnownFish(player, config)
            : GetFishForPlayer(player, config);

        if (config.ShowOnlyFish)
        {
            var allFishData = Game1.content.Load<Dictionary<string, string>>("Data/Fish");
            return allFish.Where(entry => IsFish(entry.PreviewItem, allFishData)).ToList();
        }
        return allFish;
    }

    private List<FishEntry> GetFishForPlayer(Farmer player, ModConfig config)
    {
        var location = player.currentLocation;
        if (location is null)
        {
            return new List<FishEntry>();
        }

        FishingSpotContext? spot = null;
        if (config.RespectSpawningRules && TryFindNearbyWaterTile(location, player, out var nearbyWaterTile))
        {
            // Evaluate the menu against the water tile the player is most likely to cast into.
            location.TryGetFishAreaForTile(nearbyWaterTile, out var fishAreaId, out _);
            spot = new FishingSpotContext(
                nearbyWaterTile,
                player.Tile,
                fishAreaId,
                GetWaterDepth(location, nearbyWaterTile));
        }

        var result = new Dictionary<string, FishEntry>();
        var allFishData = Game1.content.Load<Dictionary<string, string>>("Data/Fish");
        foreach (var spawn in GetSpawnRules(location))
        {
            var itemId = NormalizeItemId(spawn.ItemId);
            if (itemId is null || result.ContainsKey(itemId))
            {
                continue;
            }

            if (!SpawnMatchesCurrentContext(
                    spawn,
                    location,
                    player,
                    spot,
                    allFishData,
                    config.RespectSpawningRules))
            {
                continue;
            }

            var entry = CreateEntry(itemId, player, allFishData, config.DefaultQuality);
            if (entry is not null)
            {
                result[itemId] = entry;
            }
        }

        AddSpecialLocationFish(result, location, player, allFishData, config.DefaultQuality);
        return result.Values.OrderBy(entry => entry.DisplayName).ToList();
    }

    private static IEnumerable<SpawnFishData> GetSpawnRules(GameLocation location)
    {
        var defaultData = GameLocation.GetData("Default");
        if (defaultData?.Fish is not null)
        {
            foreach (var spawn in defaultData.Fish)
            {
                yield return spawn;
            }
        }

        var locationData = location.GetData();
        if (locationData?.Fish is not null)
        {
            foreach (var spawn in locationData.Fish)
            {
                yield return spawn;
            }
        }
    }

    private List<FishEntry> GetAllKnownFish(Farmer player, ModConfig config)
    {
        if (AllFishIdCache is null)
        {
            var ids = new HashSet<string>();
            TryAddIdsFromLocations(ids);
            TryAddIdsFromLegacyFishData(ids);
            AllFishIdCache = ids.ToList();
        }

        var allFishData = Game1.content.Load<Dictionary<string, string>>("Data/Fish");
        return AllFishIdCache
            .Select(id => CreateEntry(id, player, allFishData, config.DefaultQuality))
            .Where(entry => entry is not null)
            .Cast<FishEntry>()
            .OrderBy(entry => entry.DisplayName)
            .ToList();
    }

    private void TryAddIdsFromLocations(HashSet<string> result)
    {
        try
        {
            var locations = Game1.content.Load<Dictionary<string, LocationData>>("Data/Locations");
            foreach (var location in locations.Values)
            {
                if (location.Fish is null)
                {
                    continue;
                }

                foreach (var spawn in location.Fish)
                {
                    var itemId = NormalizeItemId(spawn.ItemId);
                    if (itemId is null)
                    {
                        continue;
                    }

                    result.Add(itemId);
                }
            }
        }
        catch (Exception ex)
        {
            Monitor.Log($"Couldn't read Data/Locations fish list: {ex.Message}", LogLevel.Trace);
        }
    }

    private void TryAddIdsFromLegacyFishData(HashSet<string> result)
    {
        try
        {
            var fishData = Game1.content.Load<Dictionary<string, string>>("Data/Fish");
            foreach (var id in fishData.Keys)
            {
                var itemId = NormalizeItemId(id);
                if (itemId is null)
                {
                    continue;
                }

                result.Add(itemId);
            }
        }
        catch (Exception ex)
        {
            Monitor.Log($"Couldn't read Data/Fish list: {ex.Message}", LogLevel.Trace);
        }
    }

    private const int FacingWaterSearchDistance = 8;
    private const int PreferredCastDistance = 4;
    private const int NearbyWaterSearchRadius = 16;

    private static bool TryFindNearbyWaterTile(GameLocation location, Farmer player, out Vector2 waterTile)
    {
        var origin = player.Tile;

        // Prefer the direction the player is facing: that's where they'll cast.
        var facingOffset = player.FacingDirection switch
        {
            Game1.up => new Vector2(0, -1),
            Game1.right => new Vector2(1, 0),
            Game1.down => new Vector2(0, 1),
            Game1.left => new Vector2(-1, 0),
            _ => Vector2.Zero
        };
        if (facingOffset != Vector2.Zero)
        {
            for (var offset = 0; offset <= FacingWaterSearchDistance; offset++)
            {
                var step = offset == 0
                    ? PreferredCastDistance
                    : PreferredCastDistance + (offset % 2 == 1 ? -(offset + 1) / 2 : offset / 2);
                if (step < 1 || step > FacingWaterSearchDistance)
                {
                    continue;
                }

                var tile = origin + facingOffset * step;
                if (IsOpenWater(location, tile))
                {
                    waterTile = tile;
                    return true;
                }
            }
        }

        for (var radius = 1; radius <= NearbyWaterSearchRadius; radius++)
        {
            for (var dx = -radius; dx <= radius; dx++)
            {
                for (var dy = -radius; dy <= radius; dy++)
                {
                    if (Math.Max(Math.Abs(dx), Math.Abs(dy)) != radius)
                    {
                        continue;
                    }

                    var tile = new Vector2(origin.X + dx, origin.Y + dy);
                    if (IsOpenWater(location, tile))
                    {
                        waterTile = tile;
                        return true;
                    }
                }
            }
        }

        waterTile = Vector2.Zero;
        return false;
    }

    private static bool IsOpenWater(GameLocation location, Vector2 tile)
    {
        try
        {
            return location.isOpenWater((int)tile.X, (int)tile.Y);
        }
        catch
        {
            return false;
        }
    }

    private static int GetWaterDepth(GameLocation location, Vector2 tile)
    {
        try
        {
            return FishingRod.distanceToLand((int)tile.X, (int)tile.Y, location);
        }
        catch
        {
            return 1;
        }
    }

    private bool SpawnMatchesCurrentContext(
        SpawnFishData spawn,
        GameLocation location,
        Farmer player,
        FishingSpotContext? spot,
        Dictionary<string, string> allFishData,
        bool respectSpawningRules)
    {
        var rod = player.CurrentTool as FishingRod;
        var usingMagicBait = rod?.HasMagicBait() == true;

        if (spawn.Season.HasValue
            && !usingMagicBait
            && spawn.Season.Value != Game1.GetSeasonForLocation(location))
        {
            return false;
        }

        if (spawn.RequireMagicBait && !usingMagicBait)
        {
            return false;
        }

        if (spawn.MinFishingLevel > player.FishingLevel)
        {
            return false;
        }

        var ignoredConditions = usingMagicBait ? GameStateQuery.MagicBaitIgnoreQueryKeys : null;
        if (!GameStateQuery.CheckConditions(
                spawn.Condition,
                location,
                player,
                null,
                null,
                null,
                ignoredConditions))
        {
            return false;
        }

        if (respectSpawningRules
            && !SpawnSpotMatcher.Matches(
                spawn.FishAreaId,
                spawn.BobberPosition,
                spawn.PlayerPosition,
                spawn.MinDistanceFromShore,
                spawn.MaxDistanceFromShore,
                spot))
        {
            return false;
        }

        var itemId = NormalizeItemId(spawn.ItemId);
        var fish = itemId is null ? null : TryCreateItem(itemId);
        if (fish is null)
        {
            return false;
        }

        if (HasReachedCatchLimit(player, fish, spawn))
        {
            return false;
        }

        allFishData.TryGetValue(fish.ItemId, out var rawFishData);
        return !respectSpawningRules
            || LegacyFishRequirements.Matches(
                rawFishData,
                Game1.timeOfDay,
                location.IsRainingHere(),
                player.FishingLevel,
                IsTrainingRod(player.CurrentTool),
                spawn.CanUseTrainingRod,
                spawn.IgnoreFishDataRequirements,
                usingMagicBait);
    }

    private void AddSpecialLocationFish(
        Dictionary<string, FishEntry> result,
        GameLocation location,
        Farmer player,
        Dictionary<string, string> allFishData,
        FishQuality quality)
    {
        if (location is not MineShaft mine || IsTrainingRod(player.CurrentTool))
        {
            return;
        }

        var itemId = mine.getMineArea() switch
        {
            MineShaft.upperArea or MineShaft.jungleArea => "(O)158",
            MineShaft.frostArea => "(O)161",
            MineShaft.lavaArea => "(O)162",
            _ => null
        };

        if (itemId is not null)
        {
            TryAddEntry(result, itemId, player, allFishData, quality);
        }
    }

    private static bool IsTrainingRod(Tool? tool)
    {
        return tool is FishingRod rod && rod.QualifiedItemId.Contains("TrainingRod", StringComparison.Ordinal);
    }

    private static bool IsFish(Item item, Dictionary<string, string> allFishData)
    {
        return allFishData.ContainsKey(item.ItemId);
    }

    private static bool HasReachedCatchLimit(Farmer player, Item fish, SpawnFishData spawn)
    {
        return spawn.CatchLimit >= 0
            && player.fishCaught.TryGetValue(fish.QualifiedItemId, out var caught)
            && caught.Length > 0
            && caught[0] >= spawn.CatchLimit;
    }

    private static void TryAddEntry(
        Dictionary<string, FishEntry> result,
        string itemId,
        Farmer player,
        Dictionary<string, string> allFishData,
        FishQuality quality)
    {
        var normalizedId = NormalizeItemId(itemId);
        if (normalizedId is null || result.ContainsKey(normalizedId))
        {
            return;
        }

        var entry = CreateEntry(normalizedId, player, allFishData, quality);
        if (entry is not null)
        {
            result[normalizedId] = entry;
        }
    }

    private static Item? TryCreateItem(string itemId)
    {
        try
        {
            return ItemRegistry.Exists(itemId) ? ItemRegistry.Create(itemId) : null;
        }
        catch
        {
            return null;
        }
    }

    private static FishEntry? CreateEntry(
        string itemId,
        Farmer player,
        Dictionary<string, string> allFishData,
        FishQuality quality)
    {
        try
        {
            var item = TryCreateItem(itemId);
            if (item is null || item.DisplayName == ItemRegistry.GetErrorItemName(itemId))
            {
                return null;
            }

            var canTrackCaughtStatus = allFishData.ContainsKey(item.ItemId);
            var isCaught = canTrackCaughtStatus
                && player.fishCaught.TryGetValue(item.QualifiedItemId, out var caught)
                && caught.Length > 0
                && caught[0] > 0;
            return new FishEntry(
                item.QualifiedItemId,
                item.DisplayName,
                item,
                GetMenuPrice(item, quality),
                canTrackCaughtStatus,
                isCaught);
        }
        catch
        {
            return null;
        }
    }

    private static int GetMenuPrice(Item item, FishQuality quality)
    {
        var resolvedQuality = quality == FishQuality.Random ? FishQuality.Normal : quality;
        var originalQuality = item.Quality;
        try
        {
            item.Quality = (int)resolvedQuality;
            return Math.Max(0, item.salePrice());
        }
        catch
        {
            return 0;
        }
        finally
        {
            item.Quality = originalQuality;
        }
    }

    private static string? NormalizeItemId(string? itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
        {
            return null;
        }

        var trimmed = itemId.Trim();
        return ItemRegistry.IsQualifiedItemId(trimmed)
            ? trimmed
            : ItemRegistry.QualifyItemId(trimmed);
    }
}
