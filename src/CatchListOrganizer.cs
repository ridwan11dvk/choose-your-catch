namespace TargetFishSync;

internal enum CatchSortMode
{
    Name,
    PriceHighToLow,
    PriceLowToHigh,
    UncaughtFirst
}

internal enum CatchFilterMode
{
    All,
    UncaughtOnly
}

internal static class CatchListOrganizer
{
    public static List<T> Apply<T>(
        IEnumerable<T> source,
        CatchSortMode sort,
        CatchFilterMode filter,
        Func<T, string> getName,
        Func<T, int> getPrice,
        Func<T, bool> canTrackCaught,
        Func<T, bool> isCaught)
    {
        var filtered = filter == CatchFilterMode.UncaughtOnly
            ? source.Where(item => canTrackCaught(item) && !isCaught(item))
            : source;
        var nameComparer = StringComparer.CurrentCultureIgnoreCase;

        return sort switch
        {
            CatchSortMode.PriceHighToLow => filtered
                .OrderByDescending(getPrice)
                .ThenBy(getName, nameComparer)
                .ToList(),
            CatchSortMode.PriceLowToHigh => filtered
                .OrderBy(getPrice)
                .ThenBy(getName, nameComparer)
                .ToList(),
            CatchSortMode.UncaughtFirst => filtered
                .OrderBy(item => !(canTrackCaught(item) && !isCaught(item)))
                .ThenBy(getName, nameComparer)
                .ToList(),
            _ => filtered.OrderBy(getName, nameComparer).ToList()
        };
    }
}
