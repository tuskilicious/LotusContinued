using System.Collections.Generic;
using Lotus.Options;

namespace Lotus.Utilities;

public readonly record struct LobbyRestrictions(int MinimumLevel, bool KickMobile, bool RequireFriendCode, bool Whitelist)
{
    private static LobbyRestrictions cached;
    private static string? cachedSummary;

    public static LobbyRestrictions Current => new(GeneralOptions.AdminOptions.KickPlayersUnderLevel,
        GeneralOptions.AdminOptions.KickMobilePlayers, GeneralOptions.AdminOptions.KickPlayersWithoutFriendcodes,
        GeneralOptions.AdminOptions.EnableWhitelist);

    public bool RejectsLevel(uint level) => MinimumLevel > 0 && level < MinimumLevel;

    public string Summary
    {
        get
        {
            if (cachedSummary != null && cached == this) return cachedSummary;
            var restrictions = new List<string>();
            if (MinimumLevel > 0) restrictions.Add($"Minimum level: {MinimumLevel}");
            if (KickMobile) restrictions.Add("Mobile devices blocked");
            if (RequireFriendCode) restrictions.Add("Friend code required");
            if (Whitelist) restrictions.Add("Whitelist on");
            cached = this;
            return cachedSummary = restrictions.Count == 0 ? "Join filters: Off (ban/block lists still apply)"
                : "Join filters: " + string.Join(" · ", restrictions);
        }
    }
}
