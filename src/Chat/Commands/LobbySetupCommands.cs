using Lotus.API.Odyssey;
using Lotus.Options;
using Lotus.Utilities;
using VentLib.Commands;
using VentLib.Commands.Attributes;

namespace Lotus.Chat.Commands;

public static class LobbySetupCommands
{
    [Command(CommandFlag.HostOnly, "lobby")]
    public static void Lobby(PlayerControl source, CommandContext context)
    {
        if (context.Args.Length == 1 && (context.Args[0] == "check" || context.Args[0] == "start"))
        {
            if (Game.State is not GameState.InLobby) { ChatHandler.Of("Use this command in the lobby.").Send(source); return; }
            if (context.Args[0] == "check") LobbyPreflight.ShowCurrent();
            else LobbyPreflight.StartReviewed();
            return;
        }
        if (context.Args.Length == 1 && context.Args[0].Equals("friends", System.StringComparison.OrdinalIgnoreCase))
        {
            if (Game.State is not GameState.InLobby)
            {
                ChatHandler.Of("Apply the friends preset from the lobby.").Send(source);
                return;
            }
            GeneralOptions.AdminOptions.ApplyFriendsPreset();
            ChatHandler.Of("Playing with friends applied to the current preset: minimum level, mobile blocking, friend-code requirement and whitelist are off. Ban/block lists and chat moderation still apply.").Send(source);
            return;
        }
        ChatHandler.Of(LobbyRestrictions.Current.Summary + "\nUse /cmd lobby friends to apply the friends preset.").Send(source);
    }

    [Command("guide")]
    public static void Guide(PlayerControl source) => ChatHandler.Of(MobileOnboarding.Guide, "Lotus quick start").Send(source);

    [Command(CommandFlag.HostOnly, "diagnostics")]
    public static void Diagnostics(PlayerControl source) => ChatHandler.Of(DiagnosticsExport.Copy()).Send(source);

    [Command("build")]
    public static void Build(PlayerControl source) => ChatHandler.Of(BuildIdentity.Display).Send(source);
}
