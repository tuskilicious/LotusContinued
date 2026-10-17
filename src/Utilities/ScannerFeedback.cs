using Hazel;
using VentLib.Networking.RPC;

namespace Lotus.Utilities;

public static class ScannerFeedback
{
    public static void Set(PlayerControl subject, PlayerControl viewer, bool enabled)
    {
        if (!AmongUsClient.Instance.AmHost || subject == null || viewer == null
            || subject.Data == null || viewer.Data == null || subject.Data.Disconnected || viewer.Data.Disconnected) return;
        byte sequence = ++subject.scannerCount;
        if (viewer.AmOwner) subject.SetScanner(enabled, sequence);
        else RpcV3.Immediate(subject.NetId, RpcCalls.SetScanner, SendOption.None)
            .Write(enabled).Write(sequence).Send(viewer.OwnerId);
    }
}
