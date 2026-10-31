namespace Lotus.Utilities;

public static partial class Utils
{
    public static PlayerControl? GetPlayerById(byte playerId)
    {
        var players = PlayerControl.AllPlayerControls;
        for (int i = 0; i < players.Count; i++)
        {
            PlayerControl player = players[i];
            if (player != null && player.PlayerId == playerId) return player;
        }
        return null;
    }
}
