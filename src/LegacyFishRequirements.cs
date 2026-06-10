using System.Globalization;

namespace TargetFishSync;

internal static class LegacyFishRequirements
{
    public static bool Matches(
        string? rawFishData,
        int timeOfDay,
        bool isRaining,
        int fishingLevel,
        bool usingTrainingRod,
        bool? canUseTrainingRod,
        bool ignoreFishDataRequirements,
        bool usingMagicBait)
    {
        if (string.IsNullOrWhiteSpace(rawFishData))
        {
            return true;
        }

        var fields = rawFishData.Split('/');
        if (fields.Length < 13)
        {
            return true;
        }

        if (usingTrainingRod)
        {
            if (canUseTrainingRod.HasValue)
            {
                if (!canUseTrainingRod.Value)
                {
                    return false;
                }
            }
            else if (TryParseInt(fields[1], out var difficulty) && difficulty >= 50)
            {
                return false;
            }
        }

        if (ignoreFishDataRequirements)
        {
            return true;
        }

        if (!usingMagicBait)
        {
            if (!MatchesTime(fields[5], timeOfDay))
            {
                return false;
            }

            if (!MatchesWeather(fields[7], isRaining))
            {
                return false;
            }
        }

        return !TryParseInt(fields[12], out var minimumFishingLevel)
            || fishingLevel >= minimumFishingLevel;
    }

    private static bool MatchesTime(string rawTimeSpans, int timeOfDay)
    {
        var values = rawTimeSpans.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (values.Length == 0 || values.Length % 2 != 0)
        {
            return true;
        }

        for (var index = 0; index < values.Length; index += 2)
        {
            if (!TryParseInt(values[index], out var start)
                || !TryParseInt(values[index + 1], out var end))
            {
                return true;
            }

            if (timeOfDay >= start && timeOfDay < end)
            {
                return true;
            }
        }

        return false;
    }

    private static bool MatchesWeather(string weather, bool isRaining)
    {
        return weather switch
        {
            "rainy" => isRaining,
            "sunny" => !isRaining,
            _ => true
        };
    }

    private static bool TryParseInt(string value, out int result)
    {
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
    }
}
