using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using Exiled.API.Features;
using MEC;
using ScpFreezer;
using PlayerRoles;
using Events = Exiled.Events.Handlers;

public static class Program
{
    static ScpFreezerPlugin plugin;
    static Player player;
    static int passed, failed;
    static int Sessions => ((IDictionary)typeof(ScpFreezerPlugin).GetField("_sessions", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(plugin)).Count;
    static void At(double t) => Timing.Advance(t);
    static void Spawn() => Events.Player.Spawn(player);
    static void Require(bool b, string detail) { if (!b) throw new Exception(detail); }
    static void Frozen() => Require(player.Effect.IsEnabled, "Expected an active freeze");
    static void Free() => Require(!player.Effect.IsEnabled, "Unexpected active freeze");
    static void Test(string name, Action check, Action<Config> configure = null, bool started = true)
    {
        Timing.Reset(); Events.Player.Reset(); Events.Server.Reset(); CustomPlayerEffects.StatusEffectBase.Reset();
        Player.List.Clear(); Log.Messages.Clear();
        Round.IsStarted = started; Round.IsEnded = false; Round.StartedAt = 0;
        player = new Player(); plugin = new ScpFreezerPlugin { Clock = () => Timing.Now };
        configure?.Invoke(plugin.Config);
        try { plugin.OnEnabled(); check(); Console.WriteLine("PASS " + name); passed++; }
        catch (Exception e) { Console.WriteLine("FAIL " + name + ": " + e); failed++; }
        finally { plugin.OnDisabled(); }
    }
    public static int Main()
    {
        Test("01 initial spawn freezes and displays remaining time", () => { Spawn(); Frozen(); Require(player.CurrentHint.Content.Contains("20"), "Missing initial countdown"); });
        Test("02 delayed spawn shares the round deadline", () => { At(2); Spawn(); Frozen(); Require(player.Effect.Duration == 18, "Duration reset on late spawn"); At(20.01); Free(); });
        Test("03 spawn after the window is not frozen", () => { At(21); Spawn(); Free(); Require(Timing.Active == 0, "Expired worker survived"); });
        Test("04 default zombie role is excluded", () => { player.Role.Type = RoleTypeId.Scp0492; Spawn(); At(1); Free(); });
        Test("05 repeat spawn keeps one worker and one session", () => { Spawn(); At(2); Spawn(); Require(Timing.Active == 1 && Sessions == 1, "Duplicate worker/session"); At(20.01); Free(); });
        Test("06 disconnect removes the session", () => { Spawn(); Events.Player.Disconnect(player); Require(Sessions == 0, "Disconnected session retained"); });
        Test("07 zero disables freezing", () => { Spawn(); At(1); Free(); Require(Timing.Active == 0, "Zero-duration worker"); }, c => c.FreezeDuration = 0);
        Test("08 null message is supported", () => { Spawn(); Frozen(); Require(player.CurrentHint == null, "Unexpected message"); }, c => c.FreezeMessage = null);
        Test("09 restart releases owned state", () => { Spawn(); At(.2); Events.Server.Restart(); Free(); Require(Sessions == 0 && Timing.Active == 0 && !player.HasHint, "Restart left state"); });
        Test("10 lobby spawn waits for RoundStarted", () => { Spawn(); At(25); Free(); Require(Timing.Active == 0, "Lobby worker"); Events.Server.Start(); Frozen(); At(45.01); Free(); }, started: false);
        Test("11 exact deadline has no trailing impulse", () => { Spawn(); At(20); Free(); });
        Test("12 spawn at 19.85 ends at 20", () => { At(19.85); Spawn(); Frozen(); At(20.00001); Free(); });
        Test("13 disable immediately clears owned effect and hint", () => { Spawn(); At(.2); plugin.OnDisabled(); Free(); Require(!player.HasHint && Sessions == 0, "Owned HUD/session remains"); });
        Test("14 existing longer external effect survives window and disable", () => { player.Effect.SetState(1, 60); Spawn(); At(20.1); Require(player.Effect.TimeLeft > 39, "External timer shortened"); plugin.OnDisabled(); Frozen(); });
        Test("15 hot enable handles an existing SCP", () => { plugin.OnDisabled(); At(2); player.IsAlive = true; plugin.OnEnabled(); Frozen(); Require(player.Effect.Duration == 18, "Hot enable restarted full timer"); });
        Test("16 NaN disables freezing and warns", () => { Spawn(); At(100); Free(); Require(Timing.Active == 0 && Log.Messages.Count(s => s.StartsWith("WARN")) == 1, "Invalid duration not rejected once"); }, c => c.FreezeDuration = float.NaN);
        Test("17 infinity disables freezing", () => { Spawn(); At(100); Free(); Require(Timing.Active == 0, "Infinite worker"); }, c => c.FreezeDuration = float.PositiveInfinity);
        Test("18 null dictionary is safe", () => { Spawn(); At(1); Free(); Require(Sessions == 0 && Log.Messages.Any(s => s.Contains("scp_freeze_settings")), "Null dictionary not handled"); }, c => c.ScpFreezeSettings = null);
        Test("19 huge duration is bounded without ushort conversion", () => { Spawn(); Frozen(); Require(player.Effect.Duration == 3600 && player.CurrentHint.Content.Contains("3600"), "Duration not bounded"); At(3600.01); Free(); }, c => c.FreezeDuration = 65536);
        Test("20 excluded role releases the previous session", () => { Spawn(); At(.2); player.Role = new() { Type = RoleTypeId.ClassD }; Spawn(); Free(); Require(Sessions == 0, "Old session retained"); });
        Test("21 removed effect is reapplied before next 0.1s tick", () => { Spawn(); At(.2); player.Effect.DisableEffect(); At(.31); Frozen(); At(20.01); Free(); });
        Test("22 short external effect expires then freezer takes over", () => { player.Effect.SetState(1, 1); Spawn(); Require(player.Effect.Duration == 1, "External effect overwritten"); At(1.11); Frozen(); At(20.01); Free(); });
        Test("23 indefinite external effect is preserved", () => { player.Effect.SetState(1, 0); Spawn(); At(20.1); plugin.OnDisabled(); Frozen(); Require(player.Effect.Duration == 0 && player.Effect.Writes == 1, "Indefinite timer changed"); });
        Test("24 external duration-only extension during freeze is preserved", () => { Spawn(); At(2); player.Effect.SetState(player.Effect.Intensity, 60); At(2.2); plugin.OnDisabled(); Frozen(); Require(player.Effect.TimeLeft > 59, "External extension removed"); });
        Test("25 external shortening does not end the startup freeze", () => { Spawn(); At(2); player.Effect.SetState(player.Effect.Intensity, .2f); At(2.31); Frozen(); At(20.01); Free(); });
        Test("26 external intensity change survives disable", () => { Spawn(); At(2); player.Effect.SetState(2, 20); plugin.OnDisabled(); Require(player.Effect.Intensity == 2, "External intensity removed"); });
        Test("27 same-duration renewal is detected by its remaining timer", () => { Spawn(); At(2); player.Effect.SetState(player.Effect.Intensity, 20); plugin.OnDisabled(); Frozen(); Require(player.Effect.TimeLeft > 19.9, "External renewal cleared"); });
        Test("28 existing unrelated hint is not overwritten", () => { player.ShowHint("Other HUD", 60); Hint h = player.CurrentHint; Spawn(); Frozen(); At(1); plugin.OnDisabled(); Require(ReferenceEquals(h, player.CurrentHint), "Other HUD overwritten"); });
        Test("29 replacement hint is not cleared on disable", () => { Spawn(); player.ShowHint("Replacement HUD", 60); Hint h = player.CurrentHint; plugin.OnDisabled(); Require(ReferenceEquals(h, player.CurrentHint), "Replacement HUD cleared"); });
        Test("30 countdown is updated and expires at the deadline", () => { Spawn(); At(2.2); Require(player.CurrentHint.Content.Contains("18"), "Stale countdown"); At(20.01); Require(!player.HasHint, "Countdown outlived window"); });
        Test("31 config reload keeps time relative to round start", () => { Spawn(); At(2); plugin.Config.FreezeDuration = 10; Events.Server.Reload(); Require(player.Effect.Duration == 8, "Reload reset deadline"); At(10.01); Free(); });
        Test("32 early round end releases state", () => { Spawn(); At(2); Events.Server.End(); Free(); Require(Timing.Active == 0 && Sessions == 0, "Round-ended worker survived"); });
        Test("33 lobby cleanup works after round timer resets", () => { Spawn(); At(2); Events.Server.Wait(); Free(); Require(!player.HasHint && Sessions == 0, "Reset timer defeated cleanup"); });
        Test("34 next round gets a fresh single window", () => { Spawn(); At(2); Events.Server.Wait(); At(25); Events.Server.Start(); Frozen(); Require(Timing.Active == 1, "Wrong worker count"); At(45.01); Free(); });
        Test("35 missed Spawned is recovered by reconciliation", () => { player.IsAlive = true; At(.11); Frozen(); Require(Sessions == 1, "Missed spawn not recovered"); });
        Test("36 disabled config does not open a window", () => { Spawn(); At(1); Free(); Require(Timing.Active == 0, "Disabled plugin worker"); }, c => c.IsEnabled = false);
        Test("37 debug provides spawn diagnostics", () => { Spawn(); Require(Log.Messages.Any(s => s.Contains("Spawned:")), "No spawn diagnostic"); }, c => c.Debug = true);
        Test("38 negative duration is rejected", () => { Spawn(); Free(); Require(Timing.Active == 0, "Negative duration worker"); }, c => c.FreezeDuration = -1);
        Test("39 missing effect is reported once without a false hint", () => { player.EffectAvailable = false; Spawn(); At(.5); Require(!player.HasHint && Log.Messages.Count(s => s.Contains("unavailable")) == 1, "Missing effect not handled"); }, c => c.Debug = true);
        Test("40 rejected effect can recover without resetting deadline", () => { player.Effect.Reject = true; Spawn(); At(1); Free(); Require(!player.HasHint, "False freeze message"); player.Effect.Reject = false; At(1.11); Frozen(); At(20.01); Free(); });
        Test("41 one failing component does not stop other players", () => { player.ThrowOnRead = true; Player other = new() { IsAlive = true }; Spawn(); At(.5); Require(other.Effect.IsEnabled && Timing.Active == 1, "Worker stopped for everyone"); Require(Log.Messages.Count(s => s.StartsWith("ERROR")) == 1, "Repeated error spam"); });
        Test("42 same-type new role replaces its session", () => { Spawn(); At(2); player.Role = new() { Type = RoleTypeId.Scp049 }; At(2.11); Frozen(); Require(Sessions == 1, "New role duplicated session"); At(20.01); Free(); });
        Test("43 null role at spawn is safe", () => { player.Role = null; Spawn(); At(.2); Require(Sessions == 0, "Null role tracked"); });
        Test("44 unsubscribed plugin ignores future events", () => { plugin.OnDisabled(); Events.Server.Start(); Spawn(); Free(); Require(Timing.Active == 0, "Disabled callbacks remain"); });
        Test("45 settings reload releases a now-excluded role", () => { Spawn(); At(1); plugin.Config.ScpFreezeSettings[RoleTypeId.Scp049] = false; Events.Server.Reload(); Free(); Require(Sessions == 0, "Excluded role still tracked"); });
        Test("46 end during effect application does not retain state", () => { player.OnEffectWrite = () => { player.OnEffectWrite = null; Events.Server.End(); }; Spawn(); At(.11); Free(); Require(Sessions == 0 && Timing.Active == 0, "Reentrant end retained state"); });
        Test("47 fractional duration ends without whole-second overshoot", () => { Spawn(); At(.051); Free(); }, c => c.FreezeDuration = .05f);
        Test("48 empty message suppresses hints", () => { Spawn(); At(1); Frozen(); Require(player.Hints.Count == 0, "Empty message emitted hint"); }, c => c.FreezeMessage = "");
        Test("49 same-duration RA write within one frame takes ownership", () => { Spawn(); At(.01); player.Effect.SetState(1, 20); plugin.OnDisabled(); Frozen(); Require(player.Effect.TimeLeft > 19.99, "Foreign same-duration write removed"); });
        Console.WriteLine($"RESULT: {passed} passed, {failed} failed. API/timing doubles; Unity and clients are not executed.");
        return failed == 0 ? 0 : 1;
    }
}
