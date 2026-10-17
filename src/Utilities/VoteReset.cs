using InnerNet;

namespace Lotus.Utilities;

public static class VoteReset
{
    public static void Send(MeetingHud hud, PlayerControl target)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (target.AmOwner)
            hud.ClearVote(new PlayerId(target.PlayerId), true);
        else
            hud.RpcClearVote(new PlayerId(target.PlayerId));
    }
}
