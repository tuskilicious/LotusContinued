# GitHub issue audit — 18 October 2026

Scope: all five upstream GitHub issues (open and closed), including their textual comments. Paginated REST enumeration confirmed five issues, excluding pull requests. The tuskilicious fork has issues disabled. Referenced screenshot attachments were not inspected; no Linux, Steam, or new multiplayer reproduction was performed. This is an audit, not a claim of resolution. Current source includes the unpublished vote/scanner hotfixes.

## Issue-by-issue disposition

| Issue | GitHub state | Assessment in this fork | Next verification |
|---|---|---|---|
| [#33: LotusCosmetics loading](https://github.com/Lotus-AU/LotusContinued/issues/33) | Open | Unresolved. Reports involve Linux and mod managers; maintainers explicitly lacked a Linux system to retest. Desktop loader searches `Paths.GameRootPath/addons`, not a manager-specific profile directory. That is a path dependency, not proof of the reported cause. Our package does not ship LotusCosmetics. | Obtain exact addon version, resolved runtime game-root path and loader exception; test plain Linux/Wine and manager profiles separately. |
| [#23: lobby creation freezing](https://github.com/Lotus-AU/LotusContinued/issues/23) | Closed | Not reproduced. Reporter withdrew the report; discussion associates it with game 17.4. No stack trace or confirmed patch identified. Local Epic logs show lobbies being created, but do not validate Steam. | Steam v19 clean-install lobby-create test with logs. |
| [#19: role/ghost flicker and walking outside map](https://github.com/Lotus-AU/LotusContinued/issues/19) | Closed | Unverified. Maintainer closed as *possibly resolved*. Shared role desync, ghost conversion and camera-reset code remain relevant; no demonstrated mapping to one cause. | Mixed vanilla-client match: role assignment, death/revive, camera reset, meetings and round transitions. Record each client's role and collision state. |
| [#18: host kicked when a player joins](https://github.com/Lotus-AU/LotusContinued/issues/18) | Closed | Historical maintainer claim of an upcoming fix, not proof for v19. User's recent lobby kicks and vote-reset disconnect are separate observations. The latest local vote fix addresses a malformed ClearVote path during a meeting, not this join report. | Official-server host/join regression with fresh settings and captured disconnect/RPC history. Do not treat old advice to use custom servers as a current verified solution. |
| [#6: website hostname typo](https://github.com/Lotus-AU/LotusContinued/issues/6) | Closed | Maintainer marked fixed. Current tracked source contains correct `beta.lotusau.top` privacy URLs and no `lotusuau` typo. Original website deployment was not independently verified. | No code change indicated; website verification is separate. |

## Additional findings from related source inspection

These are separate from the original issue causes. The subsequent local fix pass addresses all five findings; eight isolated addon regression checks cover comparison, duplicate handshakes, initialization failures and file filtering. Linux reproduction remains outstanding.

1. **High — addon version check compares a client addon to itself.** `src/Addons/AddonManager.cs`, `VerifyClientAddons`: `matchingAddon.CheckVersion(matchingAddon)` cannot detect a mismatch with `haInfo`. The host-side item is added before the later client-side comparison, and `DistinctBy(Name)` can keep that earlier clean item. Verify differing versions with the same UUID and name.
2. **Medium — repeated addon handshakes can throw.** `PlayerAddons.Add(senderControl.PlayerId, receivedAddons)` throws if that player key already exists. Verify duplicate announcements and reconnect/player-ID reuse; use replacement semantics when appropriate.
3. **Medium — failed initialization can appear loaded.** `LoadAddon` adds the instance to `Addons` before `Initialize()`. On an exception it records a failure but leaves the instance in `Addons`; `PostInitialize` is then called on it. Verify initialization failure and avoid treating partially initialized addons as successful.
4. **Medium — one addon can abort post-initialization for all following addons.** `ImportAddons` invokes `PostInitialize` without a per-addon exception boundary. Verify two addons where the first throws.
5. **Low — loader tries every file as an assembly.** `EnumerateFiles().Do(LoadAddon)` includes readmes/config files. These create misleading failed-addon entries. Filter intended assembly files, including case-insensitive extensions for Linux compatibility.

## Priority

The addon handshake and initialization repairs are implemented locally. Next run the mixed-client checks for #18 and #19; these affect playable sessions and cannot be resolved from issue closure labels alone. Keep #33 open pending actual Linux/addon reproduction. The initial audit made no code changes. The follow-up fixes are local; no GitHub issues were changed. Failed addon initialization may have external side effects which cannot be rolled back automatically.

## Evidence archive

The local, ignored `artifacts/upstream-issues.json` contains the retrieved issue bodies, comments, URLs, and timestamps. It includes user-generated content and is not a sanitized diagnostic export.
