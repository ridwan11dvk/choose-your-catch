using Microsoft.Xna.Framework;

namespace TargetFishSync;

internal readonly record struct FishingSpotContext(
    Vector2 BobberTile,
    Vector2 PlayerTile,
    string? FishAreaId,
    int WaterDepth);
