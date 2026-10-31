namespace Lotus.Roles.Interfaces;

/// <summary>
/// A guesser that picks a role by typing "/[rolename]" in chat.
/// While it has a player selected, a matching role name is a guess rather than a command, even when it is also a command alias (such as "/s").
/// </summary>
public interface IGuessInput
{
    /// <summary>True when <paramref name="roleName"/> would be taken as this guesser's role pick right now.</summary>
    public bool TakesGuess(string roleName);
}
