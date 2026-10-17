using System.Collections.Generic;
using Lotus.Chat;

namespace Lotus.Utilities;

public static class ModerationNotice
{
    private static readonly StandardLogger log = LoggerFactory.GetLogger<StandardLogger>(typeof(ModerationNotice));
    private static readonly HashSet<int> RejectedClients = new();

    public static void Reset() => RejectedClients.Clear();
    public static bool IsRejected(int clientId) => RejectedClients.Contains(clientId);
    public static void OnJoin(int clientId) => RejectedClients.Remove(clientId);

    public static void Report(string message, ModerationReason diagnosticReason = ModerationReason.Other)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        DiagnosticHistory.RecordModeration(diagnosticReason);
        log.Info(message, "Moderation");
        if (PlayerControl.LocalPlayer != null)
            ChatHandler.Of(PlainText(message), "Lobby moderation").Send(PlayerControl.LocalPlayer);
    }

    public static string PlainText(string text) => text.Replace('<', '‹').Replace('>', '›').Replace('\r', ' ').Replace('\n', ' ');

    public static void Kick(int clientId, string playerName, string reason, bool ban = false, ModerationReason diagnosticReason = ModerationReason.Other)
    {
        if (!AmongUsClient.Instance.AmHost || !RejectedClients.Add(clientId)) return;
        Report($"{playerName}: {(ban ? "banned" : "kicked")} — {reason}", diagnosticReason);
        AmongUsClient.Instance.KickPlayer(clientId, ban);
    }
}
