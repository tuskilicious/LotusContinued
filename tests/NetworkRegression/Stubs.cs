// Game and Unity boundaries are replaced only to execute the linked production logic without a game install.
namespace UnityEngine
{
    public enum KeyCode { LeftControl, Minus, Equals }
    public static class Screen { public static bool fullScreen; public static int width = 1920, height = 1080; }
    public static class Mathf { public static int Clamp(int value, int min, int max) => Math.Clamp(value, min, max); }
}
public static class ResolutionManager
{
    public static (int width, int height) LastResolution;
    public static void SetResolution(int width, int height, bool fullscreen) => LastResolution = (width, height);
}
public class StandardLogger
{
    public void Trace(string message, string? category = null) { }
    public void Debug(string message, string? category = null) { }
    public void Info(string message, string? category = null) { }
    public void Exception(string message, Exception exception) { }
}
public static class LoggerFactory { public static T GetLogger<T>(Type type) where T : new() => new(); }
public enum NetworkModes { LocalGame, OnlineGame }
public enum Platforms { Android, IPhone, StandaloneEpicPC }
public class PlatformData { public Platforms Platform; }
public class PlayerControl
{
    public static List<PlayerControl> AllPlayerControls = new();
    public static PlayerControl LocalPlayer = new();
    public byte PlayerId;
    public string name = "Player";
    public PlayerData Data = new();
    public Cosmetics cosmetics = new();
}
public class PlayerData { public string PlayerName = "Player"; public bool Disconnected; }
public class Cosmetics { public Pet CurrentPet = new(); }
public class Pet { public PetData Data = new(); }
public class PetData { public string ProductId = "pet_EmptyPet"; }
public class AmongUsClient
{
    public static AmongUsClient Instance = new();
    public bool AmHost;
    public NetworkModes NetworkMode = NetworkModes.OnlineGame;
    public int KickCount;
    public void KickPlayer(int clientId, bool ban) => KickCount++;
    public void OnPlayerJoined() { }
    public void OnPlayerLeft() { }
}
public class GameStartManager
{
    public static GameStartManager Instance = new();
    public float countDownTimer;
    public int StartCount, ResetCount;
    public Func<bool>? AllowStart;
    public void BeginGame() { if (AllowStart?.Invoke() ?? true) StartCount++; }
    public void ResetStartState() => ResetCount++;
    public void Update() { }
}
public class DestroyableSingleton<T> where T : new() { public static T Instance = new(); }
public class FriendsListManager { public bool Blocked; public bool IsPlayerBlockedUsername(string friendCode) => Blocked; }
namespace InnerNet
{
    public enum DisconnectReasons { ExitGame }
    public class ClientData
    {
        public string PlayerName = "Player", FriendCode = "player#1234";
        public int Id;
        public PlayerControl Character = null!;
        public PlatformData PlatformData = new();
    }
}
namespace HarmonyLib
{
    [AttributeUsage(AttributeTargets.Class)] public sealed class HarmonyPatchAttribute(Type type, string name) : Attribute { }
    [AttributeUsage(AttributeTargets.Parameter)] public sealed class HarmonyArgumentAttribute(int index) : Attribute { }
}
namespace VentLib.Utilities.Attributes { public sealed class LoadStaticAttribute : Attribute { } }
namespace VentLib.Utilities.Harmony.Attributes
{
    [AttributeUsage(AttributeTargets.Method)] public sealed class QuickPostfixAttribute(Type type, string name) : Attribute { }
}
namespace VentLib.Utilities.Extensions
{
    public static class CollectionExtensions
    {
        public static int IndexOf<T>(this T[] items, Func<T, bool> predicate) => Array.FindIndex(items, value => predicate(value));
    }
}
namespace VentLib.Utilities
{
    public static class Async
    {
        public static List<Action> Scheduled = new();
        public static void Schedule(Action action, float delay) => Scheduled.Add(action);
        public static void RunScheduled() { var actions = Scheduled.ToArray(); Scheduled.Clear(); foreach (var action in actions) action(); }

        public static void WaitUntil<T>(Func<T> supplier, Func<T, bool> check, Action<T> callback, int maxRetries)
        {
            T value = supplier();
            if (check(value)) callback(value);
        }
    }
}
namespace Lotus.Utilities { public class FixedUpdateLock(float duration) { public bool AcquireLock() => true; } }
namespace Lotus.Managers.Hotkeys
{
    public static class HotkeyManager
    {
        public static readonly Dictionary<UnityEngine.KeyCode, Action> Actions = new();
        public static Binding Bind(UnityEngine.KeyCode modifier, UnityEngine.KeyCode key) => new(key);
        public class Binding(UnityEngine.KeyCode key)
        {
            public Binding Name(string name) => this;
            public void Do(Action action) => Actions[key] = action;
        }
    }
}
namespace Lotus.Managers
{
    public static class PluginDataManager
    {
        public static TemplateManagerStub TemplateManager = new();
        public static WhitelistManagerStub WhitelistManager = new();
        public static BanManagerStub BanManager = new();
    }
    public class TemplateManagerStub
    {
        public int Shown;
        public void RegisterTag(string tag, string description) { }
        public string? LastTag; public object? LastData;
        public void ShowAll(string tag, PlayerControl player, object? data = null) { Shown++; LastTag = tag; LastData = data; }
    }
    public class WhitelistManagerStub { public bool Reject; public bool CheckWhitelistPlayer(PlayerControl player, InnerNet.ClientData client) => Reject; }
    public class BanManagerStub { public bool Reject; public bool CheckBanPlayer(PlayerControl player, InnerNet.ClientData client) => Reject; }
}
namespace Lotus.Managers.Templates.Models.Units
{
    public static class TemplateUnit
    {
        public static string FormatStatic(string value, object? data = null) =>
            value == "${Value}" ? ((TemplateData)data!).Value : value;
    }
}
namespace Lotus.API.Odyssey
{
    public enum GameState { InLobby, InIntro, Roaming }
    public static class Game { public static GameState State; public static object CurrentGameMode = new(); public static MatchData MatchData = new(); }
    public class MatchData
    {
        public RoleData Roles = new();
        public void RegenerateFrozenPlayers(PlayerControl player) { }
    }
    public class RoleData
    {
        public Dictionary<byte, Role> MainRoles = new();
        public Dictionary<byte, List<Role>> SubRoles = new();
    }
    public class Role { public void HandleDisconnect() { } }
}
namespace Lotus.API.Reactive
{
    public static class Hooks
    {
        public static class NetworkHooks { public static Hook ClientConnectHook = new(); }
        public static class PlayerHooks { public static Hook PlayerJoinHook = new(), PlayerDisconnectHook = new(); }
        public class Hook { public int Count; public void Propagate(object value) => Count++; }
    }
}
namespace Lotus.API.Reactive.HookEvents
{
    public record ClientConnectHookEvent(InnerNet.ClientData Client);
    public record PlayerHookEvent(PlayerControl Player);
}
namespace Lotus.Options
{
    public static class GeneralOptions { public static AdminOptionValues AdminOptions = new(); }
    public class AdminOptionValues
    {
        public bool HostGM;
        public bool KickPlayersWithoutFriendcodes, KickMobilePlayers, AutoStartEnabled, EnableWhitelist;
        public int KickPlayersUnderLevel;
        public int AutoStartPlayerThreshold = -1, AutoStartMaxTime = -1;
        public float AutoStartGameCountdown = 10;
        public Cooldown AutoCooldown = new();
    }
    public class Cooldown { public float TimeRemaining() => 20; }
}
namespace Lotus.Logging { public static class DevLogger { public static void Log(string value) { } } }
namespace Lotus.Roles.Internals { }
namespace Lotus.Roles.Internals.Enums { public enum LotusActionType { Disconnect } }
namespace Lotus.Roles.Operations
{
    public class RoleOperations
    {
        public static RoleOperations Current = new();
        public void Trigger(Lotus.Roles.Internals.Enums.LotusActionType type, PlayerControl player) { }
    }
}
namespace Lotus.RPC.CustomObjects { }
namespace xCloud { }

namespace Lotus.Chat
{
    public class ChatHandler(string message)
    {
        public static List<(string message, PlayerControl player)> Sent = new();
        public static ChatHandler Of(string message, string? title = null) => new(message);
        public void Send(PlayerControl player) => Sent.Add((message, player));
    }
}

public static class ProjectLotus { public static NormalOptions NormalOptions = new(); }
public class NormalOptions { public int NumImpostors = 1; }
namespace Lotus.GameModes.Normal.Standard { public class NormalStandardGameMode { } }
namespace Lotus.API.Player { public static class Players { public static IEnumerable<PlayerControl> GetAllPlayers() => PlayerControl.AllPlayerControls; } }
namespace Lotus.Options
{
    public static class RoleOptions { public static NeutralOptions Neutral = new(); public static NeutralOptions LoadNeutralOptions() => Neutral; }
    public class NeutralOptions { public int MinimumNeutralPassiveRoles, MaximumNeutralPassiveRoles, MinimumNeutralKillingRoles, MaximumNeutralKillingRoles; }
}
namespace Lotus.Roles
{
    [Flags] public enum RoleAbilityFlag { None, UsesPet }
    public class CustomRole { public int Count, Chance; public Lotus.Roles.Internals.Enums.RoleFlag RoleFlags; public RoleAbilityFlag RoleAbilityFlags; }
}
namespace Lotus.Roles.Internals.Enums { [Flags] public enum RoleFlag { None, IsSubrole } }
namespace Lotus.Roles.Managers.Interfaces
{
    public class IRoleManager
    {
        public static IRoleManager Current = new();
        public List<Lotus.Roles.CustomRole> Roles = new();
        public IEnumerable<Lotus.Roles.CustomRole> AllCustomRoles() => Roles;
    }
}
