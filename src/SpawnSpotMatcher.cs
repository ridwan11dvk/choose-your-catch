using Microsoft.Xna.Framework;

namespace TargetFishSync;

internal static class SpawnSpotMatcher
{
    public static bool Matches(
        string? requiredFishAreaId,
        Rectangle? bobberPosition,
        Rectangle? playerPosition,
        int minDistanceFromShore,
        int maxDistanceFromShore,
        FishingSpotContext? spot)
    {
        if (!spot.HasValue)
        {
            return true;
        }

        var value = spot.Value;
        if (!string.IsNullOrWhiteSpace(requiredFishAreaId)
            && requiredFishAreaId != value.FishAreaId)
        {
            return false;
        }

        if (bobberPosition.HasValue
            && !bobberPosition.Value.Contains((int)value.BobberTile.X, (int)value.BobberTile.Y))
        {
            return false;
        }

        if (playerPosition.HasValue
            && !playerPosition.Value.Contains((int)value.PlayerTile.X, (int)value.PlayerTile.Y))
        {
            return false;
        }

        if (minDistanceFromShore > 0 && value.WaterDepth < minDistanceFromShore)
        {
            return false;
        }

        return maxDistanceFromShore < 0 || value.WaterDepth <= maxDistanceFromShore;
    }
}
