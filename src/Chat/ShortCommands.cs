using System;
using System.Collections;
using System.Reflection;
using Lotus.Managers;
using Lotus.Utilities;
using VentLib.Commands;

namespace Lotus.Chat;

/// <summary>
/// Runs commands typed without the "/cmd" prefix ("/r", "/m", "/help"), as the in-game help and most players expect.
/// Called from Lotus's own chat patches: under IL2CPP, rewriting the chat argument in an earlier Harmony prefix does not reach VentLib's command patch.
/// </summary>
internal static class ShortCommands
{
    private static readonly StandardLogger log = LoggerFactory.GetLogger<StandardLogger>(typeof(ShortCommands));

    // VentLib keeps its command registry and runner internal.
    private const BindingFlags Internal = BindingFlags.NonPublic | BindingFlags.Instance;
    private static readonly FieldInfo? Commands = typeof(CommandRunner).GetField("Commands", Internal);
    private static readonly FieldInfo? CaseSensitiveCommands = typeof(CommandRunner).GetField("CaseSensitiveCommands", Internal);
    private static readonly MethodInfo? Execute = typeof(CommandRunner).GetMethod("Execute", Internal);
    private static readonly ConstructorInfo? NewContext = typeof(CommandContext).GetConstructor(Internal, [typeof(PlayerControl), typeof(string)]);

    /// <summary>Runs <paramref name="chatText"/> if it is a short-form command. Returns false when it is not one.</summary>
    public static bool TryRun(PlayerControl source, string chatText)
    {
        if (Execute == null || NewContext == null) return false;
        string command = CommandAlias.Normalize(chatText, CommandRunner.Prefix, IsCommand);
        if (command == chatText) return false;

        try
        {
            PluginDataManager.TemplateManager.CheckAndRunCommand(source, command);
            Execute.Invoke(CommandRunner.Instance, [NewContext.Invoke([source, command[CommandRunner.Prefix.Length..]])]);
        }
        catch (Exception exception)
        {
            log.Exception(exception);
        }
        return true;
    }

    private static bool IsCommand(string alias) =>
        Registered(Commands, alias) || Registered(CaseSensitiveCommands, alias)
        || PluginDataManager.TemplateManager?.Commands?.ContainsKey(alias) == true;

    private static bool Registered(FieldInfo? field, string alias) =>
        field?.GetValue(CommandRunner.Instance) is IDictionary commands && commands.Contains(alias);
}
