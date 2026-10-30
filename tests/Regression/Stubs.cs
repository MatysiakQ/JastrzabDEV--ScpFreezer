using System;
using System.Collections.Generic;
using System.Linq;

namespace PlayerRoles
{
    public enum RoleTypeId { ClassD, Scp049, Scp0492, Scp079, Scp096, Scp106, Scp173, Scp939, Scp3114 }
}
namespace Exiled.API.Enums { public enum EffectType { Ensnared } }
namespace Exiled.API.Interfaces { public interface IConfig { bool IsEnabled { get; set; } bool Debug { get; set; } } }
namespace CustomPlayerEffects
{
    public class StatusEffectBase
    {
        public static event Action<StatusEffectBase, byte, byte> OnIntensityChanged;
        private byte intensity;
        private double expires;
        public float Duration { get; private set; }
        public float TimeLeft => Duration == 0 ? 0 : (float)Math.Max(0, expires - MEC.Timing.Now);
        public byte Intensity
        {
            get
            {
                if (intensity != 0 && Duration > 0 && TimeLeft <= 0)
                    SetIntensity(0);
                return intensity;
            }
        }
        public bool IsEnabled => Intensity != 0;
        public bool Reject;
        public int Writes;
        public void SetState(byte value, float duration)
        {
            Writes++;
            if (value == 0 || !Reject) SetIntensity(value);
            // Like the game, same-intensity duration changes do not emit the intensity event.
            Duration = duration;
            expires = MEC.Timing.Now + duration;
        }
        public void DisableEffect() => SetIntensity(0);
        private void SetIntensity(byte value)
        {
            byte old = intensity;
            if (old == value) return;
            intensity = value;
            OnIntensityChanged?.Invoke(this, old, value);
        }
        public static void Reset() => OnIntensityChanged = null;
    }
}
namespace Exiled.API.Features
{
    public class Plugin<T> where T : new()
    {
        public T Config { get; set; } = new T();
        public virtual string Name => "";
        public virtual string Prefix => "";
        public virtual string Author => "";
        public virtual Version Version => new Version(0, 0);
        public virtual void OnEnabled() { }
        public virtual void OnDisabled() { }
    }
    public static class Log
    {
        public static readonly List<string> Messages = new();
        public static void Info(string s) => Messages.Add(s);
        public static void Warn(string s) => Messages.Add("WARN " + s);
        public static void Error(string s) => Messages.Add("ERROR " + s);
    }
    public static class Round
    {
        public static bool IsStarted;
        public static bool IsEnded;
        public static double StartedAt;
        public static TimeSpan ElapsedTime => TimeSpan.FromSeconds(MEC.Timing.Now - StartedAt);
    }
    public class Role { public PlayerRoles.RoleTypeId Type; }
    public class Hint
    {
        public string Content;
        public float Duration;
        public Hint(string content, float duration) { Content = content; Duration = duration; }
    }
    public class Player
    {
        public static readonly List<Player> List = new();
        public int Id { get; } = List.Count + 1;
        public Role Role = new() { Type = PlayerRoles.RoleTypeId.Scp049 };
        public bool IsConnected = true;
        public bool IsAlive;
        public bool EffectAvailable = true;
        public bool ThrowOnRead;
        public Action OnEffectWrite;
        public CustomPlayerEffects.StatusEffectBase Effect = new();
        public Hint CurrentHint;
        public bool HasHint => CurrentHint != null && CurrentHint.Content.Length != 0 && hintUntil > MEC.Timing.Now;
        public readonly List<(double At, string Content, float Duration)> Hints = new();
        private double hintUntil;
        public Player() { List.Add(this); }
        public bool TryGetEffect(Exiled.API.Enums.EffectType type, out CustomPlayerEffects.StatusEffectBase effect)
        {
            if (ThrowOnRead) throw new InvalidOperationException("Simulated unavailable player component");
            effect = EffectAvailable ? Effect : null;
            return effect != null;
        }
        public void EnableEffect(Exiled.API.Enums.EffectType type, byte intensity, float duration)
        {
            Effect.SetState(intensity, duration);
            OnEffectWrite?.Invoke();
        }
        public void DisableEffect(Exiled.API.Enums.EffectType type) => Effect.DisableEffect();
        public void ShowHint(string content, float duration)
        {
            CurrentHint = new Hint(content, duration);
            hintUntil = MEC.Timing.Now + duration;
            Hints.Add((MEC.Timing.Now, content, duration));
        }
    }
}
namespace Exiled.Events.EventArgs.Player
{
    public class SpawnedEventArgs { public Exiled.API.Features.Player Player; }
    public class LeftEventArgs { public Exiled.API.Features.Player Player; }
}
namespace Exiled.Events.EventArgs.Server { public class RoundEndedEventArgs { } }
namespace Exiled.Events.Handlers
{
    public static class Player
    {
        public static event Action<Exiled.Events.EventArgs.Player.SpawnedEventArgs> Spawned;
        public static event Action<Exiled.Events.EventArgs.Player.LeftEventArgs> Left;
        public static void Spawn(Exiled.API.Features.Player p) { p.IsAlive = true; Spawned?.Invoke(new() { Player = p }); }
        public static void Disconnect(Exiled.API.Features.Player p) { p.IsConnected = false; Left?.Invoke(new() { Player = p }); }
        public static void Reset() { Spawned = null; Left = null; }
    }
    public static class Server
    {
        public static event Action RoundStarted, RestartingRound, WaitingForPlayers, ReloadedConfigs;
        public static event Action<Exiled.Events.EventArgs.Server.RoundEndedEventArgs> RoundEnded;
        public static void Start() { Exiled.API.Features.Round.StartedAt = MEC.Timing.Now; Exiled.API.Features.Round.IsStarted = true; Exiled.API.Features.Round.IsEnded = false; RoundStarted?.Invoke(); }
        public static void End() { Exiled.API.Features.Round.IsEnded = true; RoundEnded?.Invoke(new()); }
        public static void Restart() => RestartingRound?.Invoke();
        public static void Wait() { Exiled.API.Features.Round.IsStarted = false; Exiled.API.Features.Round.StartedAt = MEC.Timing.Now; WaitingForPlayers?.Invoke(); }
        public static void Reload() => ReloadedConfigs?.Invoke();
        public static void Reset() { RoundStarted = null; RoundEnded = null; RestartingRound = null; WaitingForPlayers = null; ReloadedConfigs = null; }
    }
}
namespace MEC
{
    public readonly struct CoroutineHandle { public readonly int Id; public CoroutineHandle(int id) => Id = id; }
    public static class Timing
    {
        sealed class Item { public int Id; public IEnumerator<float> Iterator; public double Due; }
        static readonly Dictionary<int, Item> Items = new();
        static int nextId;
        public static double Now;
        public static int Active => Items.Count;
        public static float WaitForSeconds(float seconds) => seconds;
        public static CoroutineHandle RunCoroutine(IEnumerator<float> iterator)
        {
            Item x = new() { Id = ++nextId, Iterator = iterator };
            if (iterator.MoveNext()) { x.Due = Now + iterator.Current; Items[x.Id] = x; }
            return new CoroutineHandle(x.Id);
        }
        // Intentionally does not Dispose; plugin cleanup cannot depend on MEC doing so.
        public static void KillCoroutines(CoroutineHandle h) => Items.Remove(h.Id);
        public static void Advance(double target)
        {
            while (Items.Count != 0)
            {
                Item x = Items.Values.OrderBy(i => i.Due).First();
                if (x.Due > target) break;
                Now = x.Due;
                bool more;
                try { more = x.Iterator.MoveNext(); }
                catch { Items.Remove(x.Id); throw; }
                if (!Items.ContainsKey(x.Id)) continue;
                if (more) x.Due = Now + x.Iterator.Current;
                else Items.Remove(x.Id);
            }
            Now = target;
        }
        public static void Reset() { Items.Clear(); nextId = 0; Now = 0; }
    }
}
