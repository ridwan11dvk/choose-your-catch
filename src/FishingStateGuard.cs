namespace TargetFishSync;

internal static class FishingStateGuard
{
    public static bool IsActive(
        bool isTimingCast,
        bool isCasting,
        bool castedButBobberStillInAir,
        bool isFishing,
        bool hit,
        bool isNibbling,
        bool isReeling,
        bool pullingOutOfWater,
        bool fishCaught,
        bool showingTreasure,
        bool treasureCaught)
    {
        return isTimingCast
            || isCasting
            || castedButBobberStillInAir
            || isFishing
            || hit
            || isNibbling
            || isReeling
            || pullingOutOfWater
            || fishCaught
            || showingTreasure
            || treasureCaught;
    }
}
