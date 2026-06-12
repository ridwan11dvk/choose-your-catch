namespace TargetFishSync;

internal static class ConfigMigration
{
    private const string DefaultQualityKey = "DefaultQuality";
    private const string LegacyDefaultQuality = "Random";
    private const string CurrentDefaultQuality = "Vanilla";

    public static bool TryNormalizeDefaultQuality(IDictionary<string, object?> config)
    {
        if (!config.TryGetValue(DefaultQualityKey, out var value)
            || !string.Equals(value?.ToString(), LegacyDefaultQuality, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        config[DefaultQualityKey] = CurrentDefaultQuality;
        return true;
    }
}
