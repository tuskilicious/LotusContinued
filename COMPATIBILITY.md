# Among Us v19 migration status

Target: Among Us v19.0.0, reference package AmongUs.GameLibs.Steam 2026.9.29. Epic runtime validation is pending. Do not treat this branch as a verified stable release.

Build with PowerShell: `./scripts/build.ps1 -Dotnet dotnet` using .NET SDK 8.0.425 or compatible SDK. The script fetches pinned VentFramework source, applies the checked-in patch, builds both plugins, and runs isolated regressions. Outputs: bin/Release/net6.0/Lotus.dll and .dependencies/VentFramework-Continued/bin/Release/net6.0/VentFrameworkContinued.dll. It does not install into the game.

Verified locally: framework and Lotus Release builds succeeded with zero errors (existing warnings remain); 11/11 isolated regression checks passed, with eight failing before fixes. Tests use linked production source and game stubs; they do not exercise Unity or networking in a running game.

Changes include v12 game-option serialization, updated meeting/voting APIs, new vanilla role settings, persistent asset initialization, duplicate Reactor handshake prevention, resolution bounds, culture-independent template arithmetic, host-only administration/autostart, and safe disconnect-before-spawn handling. Additional gameplay fixes address Mastermind's delayed callback, Romantic option handling, and Airship sabotage dispatch; these require gameplay validation.

Still required: install compatible x64 BepInEx in a test Epic installation, launch and inspect patch/loading logs, test host and vanilla-client joins, play meetings and end-game transitions, verify affected roles and Airship, and investigate the first-run translation screen and remaining upstream reports. The upstream release workflow has not yet been migrated to this build script and should not be used for this branch.

API migration references: EpsTheta/Lotus commits 0c8f186202ee8ca945eb55eb084b9d0be5d16207 and 7601466859ac095cc1681b1cb386bd34efbf2947. Only relevant API adaptations were used; their unrelated changes were not imported.

Official game release: https://www.innersloth.com/new-ghost-crewmate-role-the-influencer-emergency-meeting-44/
