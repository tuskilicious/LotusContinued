using System.Collections.Generic;

namespace Lotus.Utilities;

public readonly record struct LobbySetup(int Players, bool HostSpectator, int Impostors,
    int MinNeutral, int MaxNeutral, int MinKilling, int MaxKilling, int GuaranteedRoleSlots,
    bool PetRolesEnabled, int PlayersWithoutPets);

public static class LobbyPreflightRules
{
    public static List<string> Check(LobbySetup setup)
    {
        var warnings = new List<string>();
        int active = setup.Players - (setup.HostSpectator ? 1 : 0);
        if (active < 4) warnings.Add($"Only {active} active players: standard rounds may end very quickly. Consider at least four players.");
        if (setup.Impostors <= 0) warnings.Add("No impostors configured. Check the vanilla impostor count.");
        else if (setup.Impostors * 2 >= active)
            warnings.Add($"{setup.Impostors} impostors for {active} active players may cause an immediate or very short round. Lower the impostor count.");
        if (setup.MinNeutral > setup.MaxNeutral) warnings.Add("Minimum passive neutrals exceeds the maximum. Adjust the neutral role limits.");
        if (setup.MinKilling > setup.MaxKilling) warnings.Add("Minimum neutral killers exceeds the maximum. Adjust the neutral role limits.");
        if (setup.MinNeutral + setup.MinKilling + setup.Impostors > active)
            warnings.Add("Minimum neutrals plus impostors exceed available players. Reduce the role limits.");
        if (setup.GuaranteedRoleSlots > active)
            warnings.Add($"The 100% main-role pool requests {setup.GuaranteedRoleSlots} slots for {active} players. Some configured roles cannot be assigned.");
        if (setup.PetRolesEnabled && setup.PlayersWithoutPets > 0)
            warnings.Add($"{setup.PlayersWithoutPets} players have no equipped pet (or pet data is still loading), but enabled roles use the Pet button. Equip pets before starting.");
        return warnings;
    }
}
