# Changelog

## 2.1.1 — 2026-10-30

- SF-01: only an active round can open a freeze window; RoundStarted reconciles lobby spawns.
- SF-02: leave enabled Ensnared timers untouched. A distinct intensity and guarded ownership checks protect external effects during cleanup.
- SF-03: apply only the remaining duration, so late spawns cannot add a trailing one-second impulse.
- SF-04: reconcile existing players on enable and config reload without resetting the round deadline.
- SF-05: reject negative/non-finite durations and bound the duration to 3600 seconds. Hints remove the ushort broadcast conversion.
- SF-06: treat a null role dictionary as empty and warn once per config load.
- SF-07: release identifiable owned effects and hints on disable, role changes, round end, restart and lobby transition; preserve unrelated HUD messages.
- SF-08: implement opt-in spawn/window/effect diagnostics.
- Add one worker with generation guards, per-player failure isolation and recovery for role assignments without Spawned.
- Add 49 source-based regression scenarios and a GitHub Actions workflow. These do not replace Unity/server/client tests.
- Include the net48 x64 DLL compiled against EXILED 9.12.6 and a SHA-256 checksum. Document reference DLL setup and integration limits.
