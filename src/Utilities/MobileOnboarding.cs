using System.Collections.Generic;
using InnerNet;
using Lotus.API.Odyssey;
using Lotus.Chat;
using VentLib.Utilities;

namespace Lotus.Utilities;

public static class MobileOnboarding
{
    public const string Guide = "Welcome to Lotus! The host runs the mod; these tips work with your normal game controls.\n"
        + "In a match, /cmd m explains your role. /cmd r lists roles, and /cmd build shows the host build.\n"
        + "Some roles use the Pet button for an ability: equip a pet in cosmetics. Follow your role description for Kill, Vent, or other controls.\n"
        + "Use /cmd guide to read this again. If Free Chat is unavailable, ask the host to relay the instructions.";
    private static readonly HashSet<int> Welcomed = new();
    public static void Reset() => Welcomed.Clear();
    public static void OnJoin(ClientData client, PlayerControl player)
    {
        if (!AmongUsClient.Instance.AmHost || client.PlatformData.Platform is not (Platforms.Android or Platforms.IPhone)) return;
        Async.Schedule(() =>
        {
            if (!AmongUsClient.Instance.AmHost || Game.State is not GameState.InLobby || player == null
                || player.Data == null || player.Data.Disconnected || client.Character != player
                || Utils.GetPlayerById(player.PlayerId) != player || ModerationNotice.IsRejected(client.Id)
                || !Welcomed.Add(client.Id)) return;
            ChatHandler.Of(Guide, "Lotus quick start").Send(player);
        }, 2f);
    }
}
