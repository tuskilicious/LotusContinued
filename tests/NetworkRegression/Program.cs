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

public record TemplateData(string Value);
