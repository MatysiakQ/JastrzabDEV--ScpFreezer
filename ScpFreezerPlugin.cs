using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
using MEC;
using PlayerRoles;
using System;
using System.Collections.Generic;

namespace ScpFreezer
{
    public class ScpFreezerPlugin : Plugin<Config>
    {
        public override string Name => "ScpStartFreezer";
        public override string Author => "JastrzabDev";
        public override Version Version => new Version(2, 1, 0);
        public override string Prefix => "scp_freezer";

        private readonly Dictionary<Player, CoroutineHandle> _routines = new Dictionary<Player, CoroutineHandle>();

        public override void OnEnabled()
        {
            Exiled.Events.Handlers.Player.Spawned += OnSpawned;
            Exiled.Events.Handlers.Server.RestartingRound += OnRestartingRound;
            base.OnEnabled();
        }

        public override void OnDisabled()
        {
            Exiled.Events.Handlers.Player.Spawned -= OnSpawned;
            Exiled.Events.Handlers.Server.RestartingRound -= OnRestartingRound;
            KillAllRoutines();
            base.OnDisabled();
        }

        private void OnRestartingRound() => KillAllRoutines();

        private void KillAllRoutines()
        {
            foreach (CoroutineHandle handle in _routines.Values)
                Timing.KillCoroutines(handle);

            _routines.Clear();
        }

        // Ile sekund freeza zostało, licząc od startu rundy.
        private float GetRemainingFreeze()
        {
            double elapsed = Round.IsStarted ? Round.ElapsedTime.TotalSeconds : 0;
            return (float)(Config.FreezeDuration - elapsed);
        }

        private void OnSpawned(SpawnedEventArgs ev)
        {
            Player player = ev.Player;
            if (player == null || player.Role == null) return;

            // Każdy nowy spawn zastępuje poprzednią pętlę tego gracza.
            if (_routines.TryGetValue(player, out CoroutineHandle old))
            {
                Timing.KillCoroutines(old);
                _routines.Remove(player);
            }

            RoleTypeId role = player.Role.Type;

            if (!Config.ScpFreezeSettings.TryGetValue(role, out bool shouldFreeze) || !shouldFreeze)
                return;

            if (GetRemainingFreeze() <= 0) return;

            _routines[player] = Timing.RunCoroutine(FreezeRoutine(player, role));
        }

        private IEnumerator<float> FreezeRoutine(Player player, RoleTypeId role)
        {
            // Chwila na to, żeby gra i inne pluginy skończyły nadawać rolę.
            yield return Timing.WaitForSeconds(0.1f);

            bool messageShown = false;

            while (true)
            {
                float remaining = GetRemainingFreeze();

                if (remaining <= 0
                    || player == null
                    || !player.IsConnected
                    || !player.IsAlive
                    || player.Role.Type != role)
                    break;

                // Krótki efekt odświeżany w pętli - jeśli coś go skasuje, zaraz wróci.
                player.EnableEffect(EffectType.Ensnared, 1f);

                if (!messageShown && !string.IsNullOrEmpty(Config.FreezeMessage))
                {
                    int displayTime = (int)Math.Ceiling(remaining);
                    string msg = Config.FreezeMessage.Replace("%time%", displayTime.ToString());
                    player.Broadcast((ushort)displayTime, msg);
                    messageShown = true;
                }

                yield return Timing.WaitForSeconds(0.5f);
            }

            if (player != null)
                _routines.Remove(player);
        }
    }
}