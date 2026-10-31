using System;
using HarmonyLib;
using InnerNet;
using Lotus.API.Odyssey;
using Lotus.API.Reactive;
using Lotus.API.Reactive.HookEvents;
using Lotus.Logging;
using Lotus.Managers;
using Lotus.Utilities;
using Lotus.Options;
using VentLib.Utilities;
using VentLib.Utilities.Attributes;
using VentLib.Utilities.Harmony.Attributes;
using xCloud;
using static Platforms;


namespace Lotus.Patches.Network;

[LoadStatic]
[HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnPlayerJoined))]
public class PlayerJoinPatch
{
    private static readonly StandardLogger log = LoggerFactory.GetLogger<StandardLogger>(typeof(PlayerJoinPatch));
    private static FixedUpdateLock _autostartLock = new(10f);

    static PlayerJoinPatch()
    {
        PluginDataManager.TemplateManager.RegisterTag("autostart", "Template triggered when the autostart timer begins.");
    }

    public static void Postfix(AmongUsClient __instance, [HarmonyArgument(0)] ClientData client)
    {
        ModerationNotice.OnJoin(client.Id);
        log.Trace($"{client.PlayerName} (ClientID={client.Id}) (Platform={client.PlatformData.Platform}) (FriendCode={client.FriendCode}) joined the game.", "Session");
        if (AmongUsClient.Instance.AmHost && DestroyableSingleton<FriendsListManager>.Instance.IsPlayerBlockedUsername(client.FriendCode))
        {
            ModerationNotice.Kick(client.Id, client.PlayerName, "Blocked by the host's block list.", true, ModerationReason.BlockedList);
            log.Info($"ブロック済みのプレイヤー{client.PlayerName}({client.FriendCode})をBANしました。", "BAN");
            return;
        }

        string playerName = client.PlayerName;

        Hooks.NetworkHooks.ClientConnectHook.Propagate(new ClientConnectHookEvent(client));
        Async.WaitUntil(() => client.Character, c => c != null, c => EnforceAdminSettings(client, c, playerName), maxRetries: 50);
    }

    private static void EnforceAdminSettings(ClientData client, PlayerControl player, string playerName)
    {
        if (player == null || client == null)
        {
            log.Trace($"Could not enforce admin settings for {client?.PlayerName}.");
            return;
        }

        client.PlayerName = playerName;
        player.name = playerName;
        if (!AmongUsClient.Instance.AmHost)
        {
            Hooks.PlayerHooks.PlayerJoinHook.Propagate(new PlayerHookEvent(player));
            return;
        }
        string? rejection = null;
        if (GeneralOptions.AdminOptions.KickPlayersWithoutFriendcodes && string.IsNullOrEmpty(client.FriendCode)
            && AmongUsClient.Instance.NetworkMode is not NetworkModes.LocalGame)
            rejection = "Kick Players Without Friendcode is enabled; no friend code was supplied.";
        else if (client.PlatformData.Platform is Android or IPhone && GeneralOptions.AdminOptions.KickMobilePlayers)
            rejection = "Kick Mobile Players is enabled.";

        if (rejection != null)
        {
            ModerationNotice.Kick(client.Id, playerName, rejection, diagnosticReason:
                rejection.StartsWith("Kick Mobile") ? ModerationReason.MobileBlocked : ModerationReason.MissingFriendCode);
            return;
        }

        if (PluginDataManager.WhitelistManager.CheckWhitelistPlayer(player, client))
        {
            log.Trace($"{playerName} was kicked because they weren't on the whitelist.");
            return;
        }
        if (PluginDataManager.BanManager.CheckBanPlayer(player, client))
        {
            log.Trace($"{playerName} was banned because they are on the host's banlist.");
            return;
        }

        Hooks.PlayerHooks.PlayerJoinHook.Propagate(new PlayerHookEvent(player));
        MobileOnboarding.OnJoin(client, player);
        CheckAutostart();
    }

    public static void CheckAutostart()
    {
        if (!AmongUsClient.Instance.AmHost || !GeneralOptions.AdminOptions.AutoStartEnabled) return;
        if (GeneralOptions.AdminOptions.AutoStartPlayerThreshold == -1 || PlayerControl.AllPlayerControls.Count < GeneralOptions.AdminOptions.AutoStartPlayerThreshold)
        {
            if (GeneralOptions.AdminOptions.AutoStartMaxTime == -1)
            {
                GameStartManager.Instance.ResetStartState();
                return;
            }
            DevLogger.Log("Auto Cooldown. Time Remaining: " + GeneralOptions.AdminOptions.AutoCooldown.TimeRemaining());
            GameStartManager.Instance.BeginGame();
            float timeRemaining = GeneralOptions.AdminOptions.AutoCooldown.TimeRemaining();
            GameStartManager.Instance.countDownTimer = timeRemaining;
        }
        else
        {
            if (_autostartLock.AcquireLock()) PluginDataManager.TemplateManager.ShowAll("autostart", PlayerControl.LocalPlayer);
            GameStartManager.Instance.BeginGame();
            GameStartManager.Instance.countDownTimer = GeneralOptions.AdminOptions.AutoStartGameCountdown;
        }
    }

    [QuickPostfix(typeof(GameStartManager), nameof(GameStartManager.Update))]
    private static void HookStartManager(GameStartManager __instance)
    {
        if (!AmongUsClient.Instance.AmHost || !GeneralOptions.AdminOptions.AutoStartEnabled) return;
        if (GeneralOptions.AdminOptions.AutoStartMaxTime == -1) return;
        if (Math.Abs(__instance.countDownTimer - 10f) < 0.5f && _autostartLock.AcquireLock())
            PluginDataManager.TemplateManager.ShowAll("autostart", PlayerControl.LocalPlayer);
    }
}
