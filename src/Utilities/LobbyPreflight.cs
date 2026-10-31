using System.Collections.Generic;
using System.Linq;
using Lotus.API.Odyssey;
using Lotus.API.Player;
using Lotus.Chat;
using Lotus.GameModes.Normal.Standard;
using Lotus.Options;
using Lotus.Roles;
using Lotus.Roles.Internals.Enums;
using Lotus.Roles.Managers.Interfaces;

namespace Lotus.Utilities;

public static class LobbyPreflight
{
    private static LobbySetup? reviewed;
    private static LobbySetup? approved;

    public static void Reset() { reviewed = null; approved = null; }

    public static LobbySetup Capture()
    {
        var players = Players.GetAllPlayers().Where(p => p != null && p.Data != null && !p.Data.Disconnected).ToList();
        var roles = IRoleManager.Current.AllCustomRoles().Where(r => r.Count > 0 && r.Chance > 0).ToList();
        var neutrals = RoleOptions.LoadNeutralOptions();
        return new LobbySetup(players.Count, GeneralOptions.AdminOptions.HostGM, ProjectLotus.NormalOptions.NumImpostors,
            neutrals.MinimumNeutralPassiveRoles, neutrals.MaximumNeutralPassiveRoles,
            neutrals.MinimumNeutralKillingRoles, neutrals.MaximumNeutralKillingRoles,
            roles.Where(r => r.Chance == 100 && !r.RoleFlags.HasFlag(RoleFlag.IsSubrole)).Sum(r => r.Count),
            roles.Any(r => r.RoleAbilityFlags.HasFlag(RoleAbilityFlag.UsesPet)),
            players.Count(p => (!GeneralOptions.AdminOptions.HostGM || p != PlayerControl.LocalPlayer)
                && (p.cosmetics?.CurrentPet?.Data?.ProductId is null or "pet_EmptyPet")));
    }

    public static bool CheckBeforeStart()
    {
        if (!AmongUsClient.Instance.AmHost || Game.CurrentGameMode is not NormalStandardGameMode) return true;
        var setup = Capture();
        var warnings = LobbyPreflightRules.Check(setup);
        bool allowed = warnings.Count == 0 || approved == setup;
        approved = null;
        if (allowed) { reviewed = null; return true; }
        if (reviewed != setup) Show(setup, warnings);
        return false;
    }

    public static void ShowCurrent()
    {
        if (Game.CurrentGameMode is not NormalStandardGameMode)
        {
            ChatHandler.Of("The settings check currently covers Standard mode only.").Send(PlayerControl.LocalPlayer);
            return;
        }
        var setup = Capture();
        Show(setup, LobbyPreflightRules.Check(setup));
    }

    private static void Show(LobbySetup setup, List<string> warnings)
    {
        reviewed = setup;
        ChatHandler.Of(warnings.Count == 0 ? "No issues found by the Standard-mode settings check."
            : string.Join("\n", warnings.Select(w => "• " + w))
              + "\nAdjust settings, or use /cmd lobby start to start with these warnings.", "Before starting").Send(PlayerControl.LocalPlayer);
    }

    public static void StartReviewed()
    {
        if (Game.CurrentGameMode is NormalStandardGameMode)
        {
            var setup = Capture();
            var warnings = LobbyPreflightRules.Check(setup);
            if (warnings.Count > 0 && reviewed != setup) { Show(setup, warnings); return; }
            approved = setup;
        }
        GameStartManager.Instance.BeginGame();
        approved = null;
    }
}
