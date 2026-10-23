using System;

namespace Lotus.Utilities;

public static class CommandAlias
{
    /// <summary>
    /// Rewrites the short form players type ("/r sniper") to the prefixed form ("/cmd r sniper") when the first word is a known command.
    /// Anything else, including text already using the prefix, is returned unchanged.
    /// </summary>
    public static string Normalize(string chatText, string prefix, Func<string, bool> isCommand)
    {
        string trimmed = chatText.Trim();
        if (trimmed.Length < 2 || trimmed[0] != '/' || trimmed.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return chatText;

        string command = trimmed[1..];
        string alias = command.Split(' ', 2)[0];
        return alias.Length > 0 && isCommand(alias) ? $"{prefix} {command}" : chatText;
    }
}
