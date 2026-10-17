using System.Globalization;
using System.Reflection;
using InnerNet;
using Lotus.API.Odyssey;
using Lotus.API.Reactive;
using Lotus.Managers;
using Lotus.Managers.Hotkeys;
using Lotus.Managers.Templates.Models.Units.Actions;
using Lotus.Options;
using Lotus.Patches.Network;
using Lotus.Utilities;
using UnityEngine;

var tests = new (string name, Action run)[]
{
    ("Scanner feedback uses subject sequence and local rendering for host", () =>
    {
        AmongUsClient.Instance.AmHost = true;
        var subject = new PlayerControl { scannerCount = 7 };
        var viewer = new PlayerControl { AmOwner = true, scannerCount = 20 };
        ScannerFeedback.Set(subject, viewer, true);
        Equal((byte)8, subject.scannerCount);
        Equal((byte)20, viewer.scannerCount);
        Equal(1, subject.ScannerCalls);
        Equal(0, VentLib.Networking.RPC.RpcV3.Recipients.Count);
    }),
    ("Scanner feedback sends subject sequence only to remote viewer", () =>
    {
        AmongUsClient.Instance.AmHost = true;
        var subject = new PlayerControl { scannerCount = 7 };
        ScannerFeedback.Set(subject, new PlayerControl { OwnerId = 99 }, false);
        Equal(99, VentLib.Networking.RPC.RpcV3.Recipients.Single());
        Equal<object>(false, VentLib.Networking.RPC.RpcV3.Payload[0]);
        Equal<object>((byte)8, VentLib.Networking.RPC.RpcV3.Payload[1]);
        Equal(0, subject.ScannerCalls);
    }),
    ("Scanner feedback ignores removed player data", () =>
    {
        AmongUsClient.Instance.AmHost = true;
        ScannerFeedback.Set(new PlayerControl { Data = null! }, new PlayerControl(), true);
        Equal(0, VentLib.Networking.RPC.RpcV3.Recipients.Count);
    }),
    ("Host vote reset stays local without a server RPC", () =>
    {
        AmongUsClient.Instance.AmHost = true;
        var hud = new MeetingHud();
        VoteReset.Send(hud, new PlayerControl { AmOwner = true, OwnerId = 42, PlayerId = 3 });
        Equal(1, hud.Clears);
        Equal<byte?>(3, hud.ClearedPlayer);
        Equal(true, hud.Local);
        Equal(0, VentLib.Networking.RPC.RpcV3.Recipients.Count);
    }),
    ("Remote vote reset targets only the voting client", () =>
    {
        AmongUsClient.Instance.AmHost = true;
        var hud = new MeetingHud();
        VoteReset.Send(hud, new PlayerControl { OwnerId = 77, PlayerId = 5 });
        Equal(0, hud.Clears);
        Equal(5, VentLib.Networking.RPC.RpcV3.Recipients.Single());
    }),
    ("Non-host cannot issue vote resets", () =>
    {
        var hud = new MeetingHud();
        VoteReset.Send(hud, new PlayerControl { AmOwner = true });
        VoteReset.Send(hud, new PlayerControl { OwnerId = 77, PlayerId = 5 });
        Equal(0, hud.Clears);
        Equal(0, VentLib.Networking.RPC.RpcV3.Recipients.Count);
    }),
    ("Balanced four-player settings pass preflight", () =>
    {
        Equal(0, LobbyPreflightRules.Check(new LobbySetup(4, false, 1, 0, 0, 0, 0, 4, false, 0)).Count);
    }),
    ("Preflight accounts for spectator host, role limits and pets", () =>
    {
        var warnings = LobbyPreflightRules.Check(new LobbySetup(4, true, 2, 2, 1, 2, 1, 5, true, 2));
        Equal(7, warnings.Count);
        Equal(true, warnings[0].Contains("3 active"));
    }),
    ("Preflight stops start until current warnings are reviewed", () =>
    {
        AmongUsClient.Instance.AmHost = true;
        GameStartManager.Instance.AllowStart = LobbyPreflight.CheckBeforeStart;
        Game.CurrentGameMode = new Lotus.GameModes.Normal.Standard.NormalStandardGameMode();
        PlayerControl.AllPlayerControls.Add(new PlayerControl());
        Equal(false, LobbyPreflight.CheckBeforeStart());
        Equal(1, Lotus.Chat.ChatHandler.Sent.Count);
        Equal(false, LobbyPreflight.CheckBeforeStart());
        Equal(1, Lotus.Chat.ChatHandler.Sent.Count);
        PlayerControl.AllPlayerControls.Add(new PlayerControl());
        LobbyPreflight.StartReviewed();
        Equal(0, GameStartManager.Instance.StartCount);
        LobbyPreflight.StartReviewed();
        Equal(1, GameStartManager.Instance.StartCount);
        Equal(false, LobbyPreflight.CheckBeforeStart());
    }),
    ("Mobile welcome is private and sent once", () =>
    {
        AmongUsClient.Instance.AmHost = true;
        var player = new PlayerControl { PlayerId = 7 };
        PlayerControl.AllPlayerControls.Add(player);
        var client = new ClientData { Id = 42, Character = player, PlatformData = new PlatformData { Platform = Platforms.Android } };
        MobileOnboarding.OnJoin(client, player);
        MobileOnboarding.OnJoin(client, player);
        VentLib.Utilities.Async.RunScheduled();
        Equal(1, Lotus.Chat.ChatHandler.Sent.Count);
        Equal(player, Lotus.Chat.ChatHandler.Sent[0].player);
    }),
    ("Mobile welcome cancels after disconnect, rejection or game start", () =>
    {
        AmongUsClient.Instance.AmHost = true;
        var player = new PlayerControl { PlayerId = 7 };
        PlayerControl.AllPlayerControls.Add(player);
        var client = new ClientData { Id = 42, Character = player, PlatformData = new PlatformData { Platform = Platforms.Android } };
        MobileOnboarding.OnJoin(client, player);
        player.Data.Disconnected = true;
        VentLib.Utilities.Async.RunScheduled();
        Equal(0, Lotus.Chat.ChatHandler.Sent.Count);
        player.Data.Disconnected = false;
        MobileOnboarding.OnJoin(client, player);
        Game.State = GameState.Roaming;
        VentLib.Utilities.Async.RunScheduled();
        Equal(0, Lotus.Chat.ChatHandler.Sent.Count);
        Game.State = GameState.InLobby;
        MobileOnboarding.OnJoin(client, player);
        ModerationNotice.Kick(42, "Name", "Reason");
        Lotus.Chat.ChatHandler.Sent.Clear();
        VentLib.Utilities.Async.RunScheduled();
        Equal(0, Lotus.Chat.ChatHandler.Sent.Count);
    }),
    ("Diagnostics discard identities and free-form error content", () =>
    {
        DiagnosticHistory.RecordError("NullReferenceException: Alice secret#1234 lobby ABCDEF email@example.com 192.168.1.1 C:/Users/Alice");
        DiagnosticHistory.RecordError("Alice secret#1234 custom failure");
        string report = string.Join("\n", DiagnosticHistory.Snapshot());
        Equal(true, report.Contains("NullReferenceException"));
        foreach (string sensitive in new[] { "Alice", "secret", "ABCDEF", "example", "192.168", "Users" })
            Equal(false, report.Contains(sensitive));
    }),
    ("Diagnostics event buffer stays bounded", () =>
    {
        for (int i = 0; i < 100; i++) DiagnosticHistory.RecordModeration(ModerationReason.MobileBlocked);
        Equal(20, DiagnosticHistory.Snapshot().Length);
    }),
    ("Disabled level filter accepts zero and high reported levels", () =>
    {
        var filters = new LobbyRestrictions(0, false, false, false);
        Equal(false, filters.RejectsLevel(0));
        Equal(false, filters.RejectsLevel(uint.MaxValue));
        Equal("Join filters: Off (ban/block lists still apply)", filters.Summary);
    }),
    ("Level filter rejects only below configured threshold", () =>
    {
        var filters = new LobbyRestrictions(10, false, false, false);
        Equal(true, filters.RejectsLevel(9));
        Equal(false, filters.RejectsLevel(10));
        Equal(false, filters.RejectsLevel(11));
    }),
    ("Restriction summary reflects changed settings", () =>
    {
        Equal("Join filters: Minimum level: 10 · Mobile devices blocked · Friend code required · Whitelist on",
            new LobbyRestrictions(10, true, true, true).Summary);
        Equal("Join filters: Off (ban/block lists still apply)", new LobbyRestrictions(0, false, false, false).Summary);
    }),
    ("Repeated level checks kick and notify only once per connection", () =>
    {
        AmongUsClient.Instance.AmHost = true;
        ModerationNotice.Kick(42, "Player", "Minimum level: 10");
        ModerationNotice.Kick(42, "Player", "Minimum level: 10");
        Equal(1, AmongUsClient.Instance.KickCount);
        Equal(1, Lotus.Chat.ChatHandler.Sent.Count);
        Equal(PlayerControl.LocalPlayer, Lotus.Chat.ChatHandler.Sent[0].player);
        ModerationNotice.OnJoin(42);
        ModerationNotice.Kick(42, "Player", "Minimum level: 10");
        Equal(2, AmongUsClient.Instance.KickCount);
    }),
    ("Non-host cannot kick or send moderation notices", () =>
    {
        ModerationNotice.Kick(42, "Player", "Minimum level: 10");
        Equal(0, AmongUsClient.Instance.KickCount);
        Equal(0, Lotus.Chat.ChatHandler.Sent.Count);
    }),
    ("Mobile rejection explains the responsible setting privately", () =>
    {
        AmongUsClient.Instance.AmHost = true;
        GeneralOptions.AdminOptions.KickMobilePlayers = true;
        PlayerJoinPatch.Postfix(AmongUsClient.Instance, new ClientData
        { Character = new PlayerControl(), PlatformData = new PlatformData { Platform = Platforms.IPhone } });
        Equal("Player: kicked — Kick Mobile Players is enabled.", Lotus.Chat.ChatHandler.Sent.Single().message);
        Equal(PlayerControl.LocalPlayer, Lotus.Chat.ChatHandler.Sent.Single().player);
    }),
    ("Missing friend code rejection explains the setting", () =>
    {
        AmongUsClient.Instance.AmHost = true;
        GeneralOptions.AdminOptions.KickPlayersWithoutFriendcodes = true;
        PlayerJoinPatch.Postfix(AmongUsClient.Instance, new ClientData { Character = new PlayerControl(), FriendCode = null! });
        Equal(1, AmongUsClient.Instance.KickCount);
        Equal(true, Lotus.Chat.ChatHandler.Sent.Single().message.Contains("Kick Players Without Friendcode"));
    }),
    ("Player names cannot inject rich text into moderation notices", () =>
    {
        Equal("‹size=0›Player next", ModerationNotice.PlainText("<size=0>Player\nnext"));
    }),
    ("Rejected whitelist player cannot trigger join or autostart", () => CheckRejectedJoin("whitelist")),
    ("Banned player cannot trigger join or autostart", () => CheckRejectedJoin("ban")),
    ("Blocked player cannot trigger join or autostart", () => CheckRejectedJoin("blocked")),
    ("Store resolves player-dependent variable names", () =>
    {
        Equal("score", new TActionStore("${Value}").Execute("score", new TemplateData("alice")));
        Equal("score", TActionStore.StoredVariables["alice"]);
    }),
    ("StoreAs returns resolved player-dependent variable name", () =>
    {
        Equal("bob", new TActionStoreAs("${Value}").Execute("score", new TemplateData("bob")));
        Equal("score", TActionStore.StoredVariables["bob"]);
    }),
    ("Trigger resolves tag and forwards player context", () =>
    {
        var context = new TemplateData("welcome");
        Equal("meta", new TActionTrigger("${Value}").Execute("meta", context));
        Equal("welcome", PluginDataManager.TemplateManager.LastTag);
        Equal<object?>(context, PluginDataManager.TemplateManager.LastData);
    }),
    ("Disconnect after player data removal still propagates", () =>
    {
        AmongUsClient.Instance.AmHost = true;
        Game.State = GameState.Roaming;
        PlayerLeavePatch.Postfix(AmongUsClient.Instance, new ClientData { Character = new PlayerControl { Data = null! } }, DisconnectReasons.ExitGame);
        Equal(1, Hooks.PlayerHooks.PlayerDisconnectHook.Count);
    }),
    ("Player lookup handles empty, missing, and removed players", () =>
    {
        PlayerControl.AllPlayerControls.Clear();
        Equal<PlayerControl?>(null, Utils.GetPlayerById(7));
        var player = new PlayerControl { PlayerId = 7 };
        PlayerControl.AllPlayerControls.Add(player);
        Equal(player, Utils.GetPlayerById(7));
        Equal<PlayerControl?>(null, Utils.GetPlayerById(8));
        PlayerControl.AllPlayerControls.Clear();
        Equal<PlayerControl?>(null, Utils.GetPlayerById(7));
    }),
    ("Player lookup skips null entries and preserves first match", () =>
    {
        PlayerControl.AllPlayerControls.Clear();
        var first = new PlayerControl { PlayerId = 7 };
        PlayerControl.AllPlayerControls.Add(null!);
        PlayerControl.AllPlayerControls.Add(first);
        PlayerControl.AllPlayerControls.Add(new PlayerControl { PlayerId = 7 });
        Equal(first, Utils.GetPlayerById(7));
        PlayerControl.AllPlayerControls.Clear();
    }),
    ("Resolution hotkey stays at minimum", () =>
    {
        ResolutionUtils.ResolutionIndex = 0;
        HotkeyManager.Actions[KeyCode.Minus]();
        Equal(ResolutionUtils.AllResolutions[0], ResolutionManager.LastResolution);
    }),
    ("Resolution hotkey stays at maximum", () =>
    {
        ResolutionUtils.ResolutionIndex = ResolutionUtils.AllResolutions.Length - 1;
        HotkeyManager.Actions[KeyCode.Equals]();
        Equal(ResolutionUtils.AllResolutions[^1], ResolutionManager.LastResolution);
    }),
    ("Resolution hotkeys move between adjacent options", () =>
    {
        ResolutionUtils.ResolutionIndex = 5;
        HotkeyManager.Actions[KeyCode.Equals]();
        Equal(ResolutionUtils.AllResolutions[6], ResolutionManager.LastResolution);
        HotkeyManager.Actions[KeyCode.Minus]();
        Equal(ResolutionUtils.AllResolutions[5], ResolutionManager.LastResolution);
    }),
    ("Template arithmetic preserves decimals across cultures", () =>
    {
        CultureInfo previousCulture = CultureInfo.CurrentCulture;
        try
        {
            foreach (string culture in new[] { "en-US", "de-DE", "fr-FR" })
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
                var add = new TActionAdd("0.25");
                Equal("2", add.Execute(add.Execute("1.5", null), null));
            }
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
        }
    }),
    ("Template arithmetic formats player-dependent operands", () =>
    {
        Equal("5", new TActionAdd("${Value}").Execute("2", new TemplateData("3")));
    }),
    ("Non-host joining players cannot enforce local kick settings", () =>
    {
        AmongUsClient.Instance.AmHost = false;
        GeneralOptions.AdminOptions.KickMobilePlayers = true;
        var client = new ClientData { Character = new PlayerControl(), PlatformData = new PlatformData { Platform = Platforms.Android } };
        PlayerJoinPatch.Postfix(AmongUsClient.Instance, client);
        Equal(0, AmongUsClient.Instance.KickCount);
        Equal(1, Hooks.PlayerHooks.PlayerJoinHook.Count);
    }),
    ("Non-host autostart cannot change countdown", () =>
    {
        AmongUsClient.Instance.AmHost = false;
        GeneralOptions.AdminOptions.AutoStartEnabled = true;
        GeneralOptions.AdminOptions.AutoStartPlayerThreshold = 0;
        GameStartManager.Instance.countDownTimer = 4;
        PlayerJoinPatch.CheckAutostart();
        Equal(0, GameStartManager.Instance.StartCount);
        Equal(4f, GameStartManager.Instance.countDownTimer);
    }),
    ("Disabled autostart cannot send countdown templates", () =>
    {
        AmongUsClient.Instance.AmHost = true;
        GeneralOptions.AdminOptions.AutoStartEnabled = false;
        GeneralOptions.AdminOptions.AutoStartMaxTime = 30;
        GameStartManager.Instance.countDownTimer = 10;
        typeof(PlayerJoinPatch).GetMethod("HookStartManager", BindingFlags.Static | BindingFlags.NonPublic)!
            .Invoke(null, new object[] { GameStartManager.Instance });
        Equal(0, PluginDataManager.TemplateManager.Shown);
    }),
    ("Disconnect before player spawn is safe", () =>
    {
        AmongUsClient.Instance.AmHost = true;
        Game.State = GameState.Roaming;
        PlayerLeavePatch.Postfix(AmongUsClient.Instance, new ClientData { Character = null! }, DisconnectReasons.ExitGame);
        Equal(0, Hooks.PlayerHooks.PlayerDisconnectHook.Count);
    }),
    ("Host still enforces kick settings", () =>
    {
        AmongUsClient.Instance.AmHost = true;
        GeneralOptions.AdminOptions.KickMobilePlayers = true;
        var client = new ClientData { Character = new PlayerControl(), PlatformData = new PlatformData { Platform = Platforms.Android } };
        PlayerJoinPatch.Postfix(AmongUsClient.Instance, client);
        Equal(1, AmongUsClient.Instance.KickCount);
        Equal(0, Hooks.PlayerHooks.PlayerJoinHook.Count);
    }),
    ("Host still starts configured countdown", () =>
    {
        AmongUsClient.Instance.AmHost = true;
        GeneralOptions.AdminOptions.AutoStartEnabled = true;
        GeneralOptions.AdminOptions.AutoStartPlayerThreshold = 0;
        PlayerJoinPatch.CheckAutostart();
        Equal(1, GameStartManager.Instance.StartCount);
        Equal(GeneralOptions.AdminOptions.AutoStartGameCountdown, GameStartManager.Instance.countDownTimer);
    })
};

int failed = 0;
foreach ((string name, Action run) in tests)
{
    VentLib.Networking.RPC.RpcV3.Recipients.Clear();
    Game.State = GameState.InLobby;
    Game.CurrentGameMode = new();
    PlayerControl.AllPlayerControls.Clear();
    MobileOnboarding.Reset();
    LobbyPreflight.Reset();
    DiagnosticHistory.Clear();
    VentLib.Utilities.Async.Scheduled.Clear();
    ModerationNotice.Reset();
    Lotus.Chat.ChatHandler.Sent.Clear();
    PluginDataManager.WhitelistManager = new();
    PluginDataManager.BanManager = new();
    DestroyableSingleton<FriendsListManager>.Instance = new();
    AmongUsClient.Instance = new();
    GameStartManager.Instance = new();
    GeneralOptions.AdminOptions = new();
    PluginDataManager.TemplateManager.Shown = 0;
    Hooks.PlayerHooks.PlayerJoinHook.Count = 0;
    Hooks.PlayerHooks.PlayerDisconnectHook.Count = 0;
    try
    {
        run();
        Console.WriteLine($"PASS {name}");
    }
    catch (Exception exception)
    {
        failed++;
        Console.WriteLine($"FAIL {name}: {exception.GetBaseException().Message}");
    }
}
Console.WriteLine($"{tests.Length - failed}/{tests.Length} regression checks passed.");
return failed == 0 ? 0 : 1;

static void Equal<T>(T expected, T actual)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
        throw new Exception($"Expected {expected}, got {actual}.");
}

static void CheckRejectedJoin(string reason)
{
    AmongUsClient.Instance.AmHost = true;
    GeneralOptions.AdminOptions.AutoStartEnabled = true;
    GeneralOptions.AdminOptions.AutoStartPlayerThreshold = 0;
    PluginDataManager.WhitelistManager.Reject = reason == "whitelist";
    PluginDataManager.BanManager.Reject = reason == "ban";
    DestroyableSingleton<FriendsListManager>.Instance.Blocked = reason == "blocked";
    PlayerJoinPatch.Postfix(AmongUsClient.Instance, new ClientData { Character = new PlayerControl() });
    Equal(0, Hooks.PlayerHooks.PlayerJoinHook.Count);
    Equal(0, GameStartManager.Instance.StartCount);
}

public record TemplateData(string Value);
