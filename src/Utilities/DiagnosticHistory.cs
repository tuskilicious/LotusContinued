using System;
using System.Collections.Generic;

namespace Lotus.Utilities;

public enum ModerationReason { Other, MinimumLevel, MobileBlocked, MissingFriendCode, BlockedList }

public static class DiagnosticHistory
{
    private static readonly Queue<string> Entries = new();
    private static readonly object Sync = new();
    public static void RecordModeration(ModerationReason reason) => Add("Moderation: " + reason);

    public static void RecordError(string message)
    {
        string category = "Error (message omitted for privacy)";
        foreach (string known in new[] { "NullReferenceException", "MissingMethodException", "TypeLoadException",
                     "IndexOutOfRangeException", "ArgumentOutOfRangeException", "IOException", "DuplicateConnectionDetected" })
            if (message.Contains(known, StringComparison.Ordinal)) { category = known; break; }
        Add(category);
    }

    private static void Add(string category)
    {
        lock (Sync)
        {
            if (Entries.Count == 20) Entries.Dequeue();
            Entries.Enqueue($"{DateTime.UtcNow:HH:mm:ss} UTC — {category}");
        }
    }

    public static string[] Snapshot() { lock (Sync) return Entries.ToArray(); }
    public static void Clear() { lock (Sync) Entries.Clear(); }
}
