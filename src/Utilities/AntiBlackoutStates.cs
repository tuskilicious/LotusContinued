using System;
using System.Collections.Generic;

namespace Lotus.Utilities;

public static class AntiBlackoutStates
{
    /// <summary>
    /// Sends each client its own patched view of the player states.
    /// The live data is reset to the real states before every client, so one client's patch never leaks into the next
    /// (a dead player told they are alive acts alive and gets kicked by anticheat). The real states are restored at the end.
    /// </summary>
    /// <param name="apply">Writes states into the live player data.</param>
    /// <param name="patch">Computes a client's replacements from the live player data, or null to skip that client.</param>
    /// <param name="send">Sends the live player data to a client.</param>
    public static void SendEach<TClient>(
        IEnumerable<TClient> clients,
        Dictionary<byte, (bool isDead, bool isDisconnected)> real,
        Action<Dictionary<byte, (bool isDead, bool isDisconnected)>> apply,
        Func<TClient, Dictionary<byte, (bool isDead, bool isDisconnected)>?> patch,
        Action<TClient> send)
    {
        foreach (TClient client in clients)
        {
            apply(real);
            Dictionary<byte, (bool isDead, bool isDisconnected)>? replaced = patch(client);
            if (replaced == null) continue;
            apply(replaced);
            send(client);
        }
        apply(real);
    }
}
