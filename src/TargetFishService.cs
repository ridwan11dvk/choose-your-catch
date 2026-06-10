namespace TargetFishSync;

internal sealed class TargetFishService
{
    private readonly Dictionary<long, TargetFishSelection> Selections = new();

    public TargetFishSelection? Get(long playerId)
    {
        return Selections.TryGetValue(playerId, out var selection) ? selection : null;
    }

    public void Set(long playerId, TargetFishSelection? selection)
    {
        if (selection is null)
        {
            Selections.Remove(playerId);
        }
        else
        {
            Selections[playerId] = selection;
        }
    }

    public void Remove(long playerId)
    {
        Selections.Remove(playerId);
    }

    public void Clear()
    {
        Selections.Clear();
    }

    public IEnumerable<KeyValuePair<long, TargetFishSelection>> GetAll()
    {
        return Selections;
    }
}
