using System;
using System.IO;
using System.Text;
using BepInEx;
using BepInEx.Logging;
using Lotus.API.Odyssey;
using UnityEngine;
using VentLib.Utilities.Attributes;

namespace Lotus.Utilities;

[LoadStatic]
public static class DiagnosticsExport
{
    static DiagnosticsExport() => BepInEx.Logging.Logger.Listeners.Add(new ErrorListener());

    public static string Copy()
    {
        var report = new StringBuilder(BuildIdentity.Display);
        report.AppendLine().AppendLine("Privacy: no raw log messages, names, codes, addresses, or account identifiers.");
        report.AppendLine("Installed game: " + Application.version);
        if (AmongUsClient.Instance != null)
        {
            report.AppendLine("Local host: " + AmongUsClient.Instance.AmHost);
            report.AppendLine("Game state: " + Game.State);
            report.AppendLine("Player count: " + PlayerControl.AllPlayerControls.Count);
            report.AppendLine(LobbyRestrictions.Current.Summary);
        }
        report.AppendLine("Recent events (up to 20; error details omitted):");
        foreach (string entry in DiagnosticHistory.Snapshot()) report.AppendLine(entry);
        string text = report.ToString();
        bool copied = false, saved = false;
        try { GUIUtility.systemCopyBuffer = text; copied = true; } catch { }
        try
        {
            string directory = Path.Combine(Paths.GameRootPath, "logs");
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, "lotus-diagnostics.txt"), text);
            saved = true;
        }
        catch { }
        return (copied, saved) switch
        {
            (true, true) => "Diagnostics copied and saved to logs/lotus-diagnostics.txt.",
            (true, false) => "Diagnostics copied. File could not be saved.",
            (false, true) => "Clipboard unavailable. Saved to logs/lotus-diagnostics.txt.",
            _ => "Could not copy or save diagnostics. Check clipboard and folder access."
        };
    }

    private sealed class ErrorListener : ILogListener
    {
        public BepInEx.Logging.LogLevel LogLevelFilter => BepInEx.Logging.LogLevel.Error | BepInEx.Logging.LogLevel.Fatal;
        public void LogEvent(object sender, LogEventArgs eventArgs)
        {
            if ((eventArgs.Level & (BepInEx.Logging.LogLevel.Error | BepInEx.Logging.LogLevel.Fatal)) != 0)
                DiagnosticHistory.RecordError(eventArgs.Data?.ToString() ?? "");
        }
        public void Dispose() { }
    }
}
