# Lotus 1.7.3 AU19 prerelease 1

Unofficial development build for Among Us v19.0.0. This is a prerelease: it compiles against the v19 game references but has not been validated in a running Epic game or multiplayer lobby. Existing upstream bugs may remain.

## Downloads

- **Lotus-1.7.3-au19.1-Epic-x64.zip**: complete loader, both plugins, and translations. For a clean Epic installation.
- **Lotus.dll** and **VentFrameworkContinued.dll**: replace both together if you already have the matching BepInEx 6 build 735 x64 installation.
- **SHA256SUMS.txt**: checksums for the three downloads.

## Install on Epic

1. Close Among Us. Back up existing BepInEx/config, LOTUS_DATA, and Languages if present.
2. Extract the Epic ZIP directly into your Among Us folder, beside `Among Us.exe` (not inside another subfolder).
3. Launch through Epic. The first launch may take longer while BepInEx generates game references; internet access may be needed for Unity reference downloads.
4. If loading fails, inspect `BepInEx/LogOutput.log` and include it in a bug report. This package is unsigned and does not include optional cosmetics or third-party region plugins.

## Changes and verification

Updated v19 options and meeting APIs; fixed reported Mastermind timing, Airship sabotage mapping, and several lobby/utility errors. Added player lookup and sprite caching optimizations, graphics-resource cleanup, dismissible translation prompts, and culture-independent YAML dates. Updated compatible dependencies; see `docs/DEPENDENCY_AUDIT.md` in the source for retained version constraints.

Both projects compile with zero errors; 19 isolated regression checks pass. Existing warnings remain. No measured FPS claim and no claim that all bugs are fixed. In-game validation, mixed-client multiplayer, and new vanilla role behavior remain to be checked.

Includes the official BepInEx Unity IL2CPP x64 loader 6.0.0-be.735+5fef357 and translations from Lotus-AU/Languages commit 1cb89d1fe013407cc13e472e1472e95ac8ad3056. Among Us itself is not included.
