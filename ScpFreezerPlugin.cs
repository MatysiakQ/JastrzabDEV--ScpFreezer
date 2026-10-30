using CustomPlayerEffects;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
using Exiled.Events.EventArgs.Server;
using MEC;
using PlayerRoles;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace ScpFreezer
{
    public class ScpFreezerPlugin : Plugin<Config>
    {
        private const float TickInterval = 0.1f;
        private const float HintLifetime = 0.3f;
        private const float TimerTolerance = 0.1f;
        // Ensnared is binary (speed multiplier/limit are zero at any positive intensity).
        // A distinct intensity lets normal RA/API writes take ownership explicitly.
        private const byte OwnIntensity = byte.MaxValue;

        public override string Name => "ScpStartFreezer";
        public override string Author => "JastrzabDev";
        public override Version Version => new Version(2, 1, 1);
        public override string Prefix => "scp_freezer";

        private readonly Dictionary<Player, FreezeSession> _sessions = new Dictionary<Player, FreezeSession>();
        private readonly HashSet<Player> _writingEffect = new HashSet<Player>();
        private readonly HashSet<Player> _loggedFailures = new HashSet<Player>();
        private Dictionary<RoleTypeId, bool> _settings;
        private CoroutineHandle _routine;
        private bool _running;
        private bool _enabled;
        private bool _windowOpen;
        private float _duration;
        private int _generation;
        // Effect.TimeLeft advances with Unity deltaTime, which may be capped during a lag spike.
        // Use the same clock for ownership checks; the round deadline remains Round.ElapsedTime.
        internal Func<double> Clock = () => UnityEngine.Time.timeAsDouble;

        private sealed class FreezeSession
        {
            public object Role;
            public StatusEffectBase Effect;
            public bool OwnsEffect;
            public float AppliedDuration;
            public double AppliedAt;
            public Hint OwnHint;
            public double NextHintAt;
            public bool EffectFailureLogged;
        }

        public override void OnEnabled()
        {
            ValidateConfig();
            _enabled = true;
            Exiled.Events.Handlers.Player.Spawned += OnSpawned;
            Exiled.Events.Handlers.Player.Left += OnLeft;
            StatusEffectBase.OnIntensityChanged += OnEffectIntensityChanged;
            Exiled.Events.Handlers.Server.RoundStarted += OnRoundStarted;
            Exiled.Events.Handlers.Server.RoundEnded += OnRoundEnded;
            Exiled.Events.Handlers.Server.RestartingRound += StopWindow;
            Exiled.Events.Handlers.Server.WaitingForPlayers += StopWindow;
            Exiled.Events.Handlers.Server.ReloadedConfigs += OnReloadedConfigs;
            base.OnEnabled();
            StartWindow();
        }

        public override void OnDisabled()
        {
            _enabled = false;
            Exiled.Events.Handlers.Player.Spawned -= OnSpawned;
            Exiled.Events.Handlers.Player.Left -= OnLeft;
            StatusEffectBase.OnIntensityChanged -= OnEffectIntensityChanged;
            Exiled.Events.Handlers.Server.RoundStarted -= OnRoundStarted;
            Exiled.Events.Handlers.Server.RoundEnded -= OnRoundEnded;
            Exiled.Events.Handlers.Server.RestartingRound -= StopWindow;
            Exiled.Events.Handlers.Server.WaitingForPlayers -= StopWindow;
            Exiled.Events.Handlers.Server.ReloadedConfigs -= OnReloadedConfigs;
            StopWindow();
            base.OnDisabled();
        }

        private void ValidateConfig()
        {
            _duration = Config.FreezeDuration;
            if (float.IsNaN(_duration) || float.IsInfinity(_duration) || _duration < 0)
            {
                Log.Warn("[ScpFreezer] Invalid freeze_duration; freezing is disabled until the config is corrected and reloaded.");
                _duration = 0;
            }
            else if (_duration > Config.MaximumFreezeDuration)
            {
                Log.Warn("[ScpFreezer] freeze_duration exceeds 3600 seconds; using 3600 seconds.");
                _duration = Config.MaximumFreezeDuration;
            }

            if (Config.ScpFreezeSettings == null)
                Log.Warn("[ScpFreezer] scp_freeze_settings is null; no roles will be frozen.");

            _settings = Config.ScpFreezeSettings == null
                ? new Dictionary<RoleTypeId, bool>()
                : new Dictionary<RoleTypeId, bool>(Config.ScpFreezeSettings);
        }

        private void OnRoundStarted() => StartWindow();
        private void OnRoundEnded(RoundEndedEventArgs ev) => StopWindow();

        private void OnReloadedConfigs()
        {
            StopWindow();
            ValidateConfig();
            StartWindow();
        }

        private float Remaining()
        {
            if (!_enabled || !Config.IsEnabled || !Round.IsStarted || Round.IsEnded)
                return 0;

            return (float)Math.Max(0, _duration - Round.ElapsedTime.TotalSeconds);
        }

        private void StartWindow()
        {
            StopWindow();
            if (Remaining() <= 0)
                return;

            _windowOpen = true;
            DebugLog("Freeze window opened; remaining=" + Remaining().ToString("F2", CultureInfo.InvariantCulture) + "s.");
            _running = true;
            int generation = _generation;
            CoroutineHandle routine = Timing.RunCoroutine(WindowRoutine(generation));
            if (_generation == generation)
                _routine = routine;
        }

        private IEnumerator<float> WindowRoutine(int generation)
        {
            try
            {
                while (generation == _generation && _windowOpen && Remaining() > 0)
                {
                    // Also catches custom role assignments which did not emit Spawned.
                    foreach (Player player in Player.List.ToArray())
                    {
                        if (generation != _generation)
                            yield break;
                        RefreshSafely(player);
                    }

                    if (generation != _generation)
                        yield break;

                    foreach (Player player in _sessions.Keys.ToArray())
                        if (!IsSelected(player))
                            Release(player);

                    float remaining = Remaining();
                    if (remaining <= 0)
                        break;

                    yield return Timing.WaitForSeconds(Math.Min(TickInterval, remaining));
                }
            }
            finally
            {
                if (generation == _generation)
                {
                    _running = false;
                    _windowOpen = false;
                    ReleaseAll();
                    DebugLog("Freeze window closed.");
                }
            }
        }

        private void StopWindow()
        {
            bool wasOpen = _windowOpen;
            _generation++;
            _windowOpen = false;
            if (_running)
            {
                _running = false;
                Timing.KillCoroutines(_routine);
            }

            // MEC cancellation need not run the iterator's finally block.
            ReleaseAll();
            _loggedFailures.Clear();
            if (wasOpen)
                DebugLog("Freeze window stopped.");
        }

        private void OnSpawned(SpawnedEventArgs ev)
        {
            if (ev.Player == null)
                return;

            DebugLog("Spawned: player=" + ev.Player.Id + ", role=" + (ev.Player.Role == null ? "<none>" : ev.Player.Role.Type.ToString()) + ", selected=" + IsSelected(ev.Player) + ".");
            _loggedFailures.Remove(ev.Player);
            Release(ev.Player);
            if (_windowOpen)
                RefreshSafely(ev.Player);
        }

        private void OnLeft(LeftEventArgs ev)
        {
            Release(ev.Player);
            if (ev.Player != null)
                _loggedFailures.Remove(ev.Player);
        }

        private bool IsSelected(Player player)
        {
            return player != null && player.IsConnected && player.IsAlive && player.Role != null
                && _settings.TryGetValue(player.Role.Type, out bool selected) && selected;
        }

        private void RefreshSafely(Player player)
        {
            try
            {
                Refresh(player);
            }
            catch (Exception exception)
            {
                Release(player);
                if (player != null && _loggedFailures.Add(player))
                    Log.Error("[ScpFreezer] Could not update a player's freeze: " + exception);
            }
        }

        private void Refresh(Player player)
        {
            float remaining = Remaining();
            if (!_windowOpen || remaining <= 0 || !IsSelected(player))
            {
                Release(player);
                return;
            }

            if (_sessions.TryGetValue(player, out FreezeSession session) && !ReferenceEquals(session.Role, player.Role))
            {
                Release(player);
                session = null;
            }

            if (session == null)
            {
                session = new FreezeSession { Role = player.Role };
                _sessions.Add(player, session);
                DebugLog("Tracking freeze: player=" + player.Id + ".");
            }

            bool frozen = false;
            if (player.TryGetEffect(EffectType.Ensnared, out StatusEffectBase effect))
            {
                if (session.OwnsEffect && !StillOwnsEffect(session, effect))
                    session.OwnsEffect = false;

                // Never reset an existing effect: it may belong to RA or another plugin.
                if (!effect.IsEnabled)
                {
                    session.Effect = effect;
                    session.OwnsEffect = false;
                    _writingEffect.Add(player);
                    try
                    {
                        player.EnableEffect(EffectType.Ensnared, OwnIntensity, remaining);
                    }
                    finally
                    {
                        _writingEffect.Remove(player);
                    }

                    session.AppliedDuration = remaining;
                    session.AppliedAt = Clock();
                    session.OwnsEffect = effect.IsEnabled && effect.Intensity == OwnIntensity
                        && effect.Duration == remaining && Math.Abs(effect.TimeLeft - remaining) <= TimerTolerance;

                    // A synchronous effect callback may have ended the round or changed the role.
                    if (!_windowOpen || !_sessions.TryGetValue(player, out FreezeSession currentSession)
                        || !ReferenceEquals(session, currentSession) || !ReferenceEquals(session.Role, player.Role))
                    {
                        if (session.OwnsEffect && StillOwnsEffect(session, effect))
                            player.DisableEffect(EffectType.Ensnared);
                        return;
                    }
                }

                if (!effect.IsEnabled && !session.EffectFailureLogged)
                {
                    session.EffectFailureLogged = true;
                    DebugLog("Ensnared was rejected for player=" + player.Id + "; check spawn protection or effect handlers.");
                }
                frozen = effect.IsEnabled;
            }
            else if (!session.EffectFailureLogged)
            {
                session.EffectFailureLogged = true;
                DebugLog("Ensnared is unavailable for player=" + player.Id + ".");
            }

            if (frozen)
                RefreshHint(player, session, remaining);
            else
                ReleaseHint(player, session);
        }

        private void OnEffectIntensityChanged(StatusEffectBase effect, byte previous, byte current)
        {
            foreach (KeyValuePair<Player, FreezeSession> pair in _sessions)
                if (!_writingEffect.Contains(pair.Key) && ReferenceEquals(pair.Value.Effect, effect))
                    pair.Value.OwnsEffect = false;
        }

        private bool StillOwnsEffect(FreezeSession session, StatusEffectBase effect)
        {
            if (!ReferenceEquals(session.Effect, effect) || !effect.IsEnabled || effect.Intensity != OwnIntensity
                || effect.Duration != session.AppliedDuration)
                return false;

            // Duration-only writes do not raise intensity-change events.
            double expected = Math.Max(0, session.AppliedDuration - (Clock() - session.AppliedAt));
            return Math.Abs(effect.TimeLeft - expected) <= TimerTolerance;
        }

        private void RefreshHint(Player player, FreezeSession session, float remaining)
        {
            double now = Round.ElapsedTime.TotalSeconds;
            if (now < session.NextHintAt || string.IsNullOrEmpty(Config.FreezeMessage))
                return;

            // Leave an unrelated active hint alone; do not clear other plugins' broadcasts.
            if (player.HasHint && !ReferenceEquals(player.CurrentHint, session.OwnHint))
                return;

            string message = Config.FreezeMessage.Replace("%time%", ((int)Math.Ceiling(remaining)).ToString(CultureInfo.InvariantCulture));
            player.ShowHint(message, Math.Min(HintLifetime, remaining));
            Hint current = player.CurrentHint;
            session.OwnHint = current != null && current.Content == message ? current : null;
            session.NextHintAt = now + TickInterval;
        }

        private void Release(Player player)
        {
            if (player == null || !_sessions.TryGetValue(player, out FreezeSession session))
                return;

            _sessions.Remove(player);
            try
            {
                if (!player.IsConnected)
                    return;

                if (session.OwnsEffect && player.TryGetEffect(EffectType.Ensnared, out StatusEffectBase effect)
                    && StillOwnsEffect(session, effect))
                    player.DisableEffect(EffectType.Ensnared);

                ReleaseHint(player, session);
            }
            catch (Exception exception)
            {
                Log.Error("[ScpFreezer] Could not release a player's freeze: " + exception);
            }
        }

        private void ReleaseAll()
        {
            foreach (Player player in _sessions.Keys.ToArray())
                Release(player);
        }

        private static void ReleaseHint(Player player, FreezeSession session)
        {
            if (session.OwnHint != null && ReferenceEquals(player.CurrentHint, session.OwnHint))
                player.ShowHint(string.Empty, 0.01f);
            session.OwnHint = null;
        }

        private void DebugLog(string message)
        {
            if (Config.Debug)
                Log.Info("[ScpFreezer:DEBUG] " + message);
        }
    }
}
