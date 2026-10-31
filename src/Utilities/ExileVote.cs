using System;
using System.Collections.Generic;
using System.Linq;

namespace Lotus.Utilities;

public static class ExileVote
{
    public const byte NoExile = byte.MaxValue;

    /// <summary>
    /// Picks who is exiled from vote counts (key 255 is the skip vote).
    /// On a tie, <paramref name="pickTied"/> (random tie mode) chooses one of the tied entries but the result stays a tie,
    /// so roles that run afterwards (Tie Breaker, Dictator, Jailor) can still override or cancel it.
    /// </summary>
    public static (byte exiled, bool isTie, HashSet<byte> tied) Calculate(IReadOnlyDictionary<byte, int> voteCounts, Func<List<byte>, byte>? pickTied)
    {
        List<KeyValuePair<byte, int>> sorted = voteCounts.OrderByDescending(kv => kv.Value).ToList();
        if (sorted.Count == 0) return (NoExile, false, new HashSet<byte>());

        int mostVotes = sorted[0].Value;
        HashSet<byte> tied = sorted.Where(kv => kv.Value == mostVotes).Select(kv => kv.Key).ToHashSet();
        if (tied.Count < 2) return (sorted[0].Key, false, tied);
        return (pickTied?.Invoke(tied.ToList()) ?? NoExile, true, tied);
    }
}
