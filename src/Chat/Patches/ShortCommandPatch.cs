using System.Collections;
using System.Reflection;
using HarmonyLib;
using VentLib.Utilities.Extensions;
using Lotus.Managers;
using Lotus.Utilities;
using VentLib.Commands;

namespace Lotus.Chat.Patches;

/// <summary>
/// Lets players type commands without the "/cmd" prefix ("/r", "/m", "/help"), as the in-game help and most players expect.
/// Runs before VentLib's command patches so the rewritten text is handled exactly like the prefixed form.
/// </summary>
internal static class ShortCommandPatch
{
    // VentLib keeps registered commands internal; read them so only real command names are rewritten.
    private static readonly FieldInfo? Commands = typeof(CommandRunner).GetField("Commands", BindingFlags.NonPublic | BindingFlags.Instance);
    private static readonly FieldInfo? CaseSensitiveCommands = typeof(CommandRunner).GetField("CaseSensitiveCommands", BindingFlags.NonPublic | BindingFlags.Instance);

    private static bool IsCommand(string alias) =>
        Registered(Commands, alias) || Registered(CaseSensitiveCommands, alias)
        || PluginDataManager.TemplateManager?.Commands?.ContainsKey(alias) == true;

    private static bool Registered(FieldInfo? field, string alias) =>
        CommandRunner.Instance != null && field?.GetValue(CommandRunner.Instance) is IDictionary commands && commands.Contains(alias);

    private static string Normalize(string chatText) => CommandAlias.Normalize(chatText, CommandRunner.Prefix, IsCommand);

    [HarmonyPriority(Priority.First + 1)]
    [HarmonyPatch(typeof(ChatController), nameof(ChatController.AddChat))]
    private static class PlayerCommands
    {
        private static void Prefix(PlayerControl sourcePlayer, ref string chatText)
        {
            if (AmongUsClient.Instance.AmHost && !sourcePlayer.IsHost()) chatText = Normalize(chatText);
        }
    }

    [HarmonyPriority(Priority.First + 1)]
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RpcSendChat))]
    private static class HostCommands
    {
        private static void Prefix(PlayerControl __instance, ref string chatText)
        {
            if (AmongUsClient.Instance.AmHost && __instance.IsHost()) chatText = Normalize(chatText);
        }
    }
}
