using StardewModdingAPI;
using StardewModdingAPI.Utilities;

namespace TargetFishSync;

internal sealed class ModConfig
{
    public KeybindList OpenMenuKey { get; set; } = KeybindList.Parse("Q");

    public bool AllowAllFish { get; set; }
    
    public bool ShowOnlyFish { get; set; } = false;

    public bool RespectSpawningRules { get; set; } = true;

    public FishQuality DefaultQuality { get; set; } = FishQuality.Vanilla;
}

internal enum FishQuality
{
    Vanilla = -1,
    Normal = 0,
    Silver = 1,
    Gold = 2,
    Iridium = 4
}
