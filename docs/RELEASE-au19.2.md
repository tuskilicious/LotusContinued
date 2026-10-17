# Lotus 1.7.3 AU19 prerelease 2

Unofficial development build for Among Us v19.0.0. This prerelease targets the v19 game references. A previous build loaded on Epic, but a four-device session reported lobby kicks. These changes have not yet been verified in a live multiplayer session. Existing upstream bugs may remain.

## Downloads

- **Lotus-1.7.3-au19.2-Epic-x64.zip**: complete loader, both plugins, and translations. For a clean Epic installation.
- **Lotus.dll** and **VentFrameworkContinued.dll**: replace both together if you already have the matching BepInEx 6 build 735 x64 installation.
- **SHA256SUMS.txt**: checksums for the three downloads.

## Install on Epic

1. Close Among Us. Back up existing BepInEx/config, LOTUS_DATA, and Languages if present.
2. Extract the Epic ZIP directly into your Among Us folder, beside `Among Us.exe` (not inside another subfolder).
3. Launch through Epic. The first launch may take longer while BepInEx generates game references; internet access may be needed for Unity reference downloads.
4. If loading fails, inspect `BepInEx/LogOutput.log` and include it in a bug report. This package is unsigned and does not include optional cosmetics or third-party region plugins.

## Lobby setup

- The host sees active join restrictions below the build information in the lobby.
- `/cmd lobby friends` applies the Playing with friends preset to the active settings: minimum level, mobile blocking, friend-code requirement, and whitelist are disabled. Existing ban/block lists and chat moderation remain active.
- `/cmd lobby` displays restrictions; `/cmd build` reports the host's assembly version, prerelease number, build ID, and target game version.
- New configurations default the minimum-level filter to Disabled. Existing configurations are preserved: use the friends command to change them.
- Automatic moderation reports the reason privately to the host and in logs. Repeated level checks produce one kick/notice per connection.

## Changes and verification

Includes prerelease 1 changes plus lobby quality-of-life improvements and fixes for rejected-player join processing, player-dependent template actions, and disconnects after player data removal.

Both projects compile with zero errors; 34 isolated regression checks pass. Existing warnings remain. UI positioning and a complete mixed-device match still need in-game validation. The build ID is derived from the assembly module ID so separately compiled DLLs can be distinguished even with the same version number.

Includes the official BepInEx Unity IL2CPP x64 loader 6.0.0-be.735+5fef357 and translations from Lotus-AU/Languages commit 1cb89d1fe013407cc13e472e1472e95ac8ad3056. Among Us itself is not included.
