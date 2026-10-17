using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace GungeonTogether.Lan
{
    // The game supplies its own sound banks. Only event IDs, emitter positions and
    // weapon/foley switch values cross the network; local volume settings stay local.
    public sealed class AudioReplication : IDisposable
    {
        private sealed class Source
        {
            public uint Id;
            public GameObject Object;
            public float Touched;
            public readonly Dictionary<uint, uint> Switches = new Dictionary<uint, uint>();
        }
        private readonly Dictionary<int, Source> sources = new Dictionary<int, Source>();
        private readonly Dictionary<uint, Source> receivers = new Dictionary<uint, Source>();
        private readonly List<SoundCue> pending = new List<SoundCue>();
        private readonly List<int> expired = new List<int>();
        private readonly List<uint> expiredReceivers = new List<uint>();
        private readonly SoundSequence sequence = new SoundSequence();
        private uint nextSource, nextBatch;
        private float nextFlush, oldest;
        [ThreadStatic] private static bool replaying;
        public int Captured { get; private set; }
        public int Dispatched { get; private set; }
        public int Dropped { get; private set; }
        public int FailedEvents { get; private set; }
        public int SwitchesApplied { get; private set; }
        public int Queued { get { return pending.Count; } }
        public void ObserveClock(int epoch, double senderTime) { sequence.ObserveClock(epoch, senderTime, Time.realtimeSinceStartup); }
        private static DungeonSession Session { get { return DungeonPlugin.Instance == null ? null : DungeonPlugin.Instance.Session; } }

        private static bool BattleEvent(object id)
        {
            string name = id as string;
            if (name == null) return id is uint && (uint)id != 0;
            // Music, UI and ambient loops are still owned by the local loaded scene.
            string[] categories = { "WPN_", "ENM_", "OBJ_", "CHR_", "FS_", "VO_", "PLR_", "BOSS_" };
            foreach (string category in categories)
                if (name.StartsWith("Play_" + category, StringComparison.OrdinalIgnoreCase) || name.StartsWith("Stop_" + category, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }
        private static uint Id(object value) { return value is uint ? (uint)value : AkSoundEngine.GetIDFromString((string)value); }
        public void PlayHurt(PlayerController player, int character)
        {
            if (player == null) return;
            replaying = true;
            try { AkSoundEngine.SetSwitch("CHR_Player", CharacterCatalog.Identity(character), player.gameObject); AkSoundEngine.PostEvent("Play_CHR_general_hurt_01", player.gameObject); }
            finally { replaying = false; }
        }
        private Source GetSource(GameObject obj)
        {
            if (obj == null) return null;
            Source source;
            if (!sources.TryGetValue(obj.GetInstanceID(), out source)) {
                if (sources.Count >= 256) return null;
                source = new Source { Id = ++nextSource, Object = obj };
                Gun gun = obj.GetComponent<Gun>();
                if (gun != null && !string.IsNullOrEmpty(gun.gunSwitchGroup)) source.Switches[AkSoundEngine.GetIDFromString("WPN_Guns")] = AkSoundEngine.GetIDFromString(gun.gunSwitchGroup);
                PlayerController player = obj.GetComponent<PlayerController>();
                if (player != null) source.Switches[AkSoundEngine.GetIDFromString("CHR_Player")] = AkSoundEngine.GetIDFromString(player.characterIdentity.ToString());
                sources.Add(obj.GetInstanceID(), source);
            }
            source.Touched = Time.realtimeSinceStartup; return source;
        }
        private void Capture(object id, GameObject obj)
        {
            if (pending.Count >= 256) { Dropped++; return; }
            Source source = GetSource(obj); if (source == null) return;
            Vector3 position = obj.transform.position;
            PlayerController guest = GameManager.Instance.SecondaryPlayer;
            // The scene-effects damage counter guarantees the guest hurt cue
            // alongside its native screen flash, even when short audio batches
            // expire. Do not play the same cue twice through both paths.
            if (guest != null && obj == guest.gameObject && Id(id) == AkSoundEngine.GetIDFromString("Play_CHR_general_hurt_01")) return;
            if (guest != null && Vector2.Distance(position.XY(), guest.CenterPosition) > 80) return;
            SoundCue cue = new SoundCue { Event = Id(id), Emitter = source.Id, X = position.x, Y = position.y };
            foreach (KeyValuePair<uint, uint> pair in source.Switches) cue.Switches.Add(new SoundSwitch { Group = pair.Key, Value = pair.Value });
            if (pending.Count == 0) oldest = Time.realtimeSinceStartup;
            pending.Add(cue); Captured++;
        }
        public void Flush(int epoch, LanTransport transport)
        {
            float now = Time.realtimeSinceStartup;
            expired.Clear();
            foreach (KeyValuePair<int, Source> pair in sources) if (pair.Value.Object == null || now - pair.Value.Touched > 10) expired.Add(pair.Key);
            foreach (int id in expired) sources.Remove(id);
            if (now < nextFlush || pending.Count == 0) return;
            nextFlush = now + 1f / 30f;
            if (now - oldest > 0.25f || transport.OldestPendingMilliseconds > 150) { Dropped += pending.Count; pending.Clear(); return; }
            SoundBatch batch = new SoundBatch { Epoch = epoch, Sequence = ++nextBatch, SentAt = now };
            int count = Math.Min(SoundBatch.MaxCues, pending.Count);
            batch.Cues.AddRange(pending.GetRange(0, count)); pending.RemoveRange(0, count);
            transport.Send(batch.Encode());
        }
        public void Receive(SoundBatch batch, int epoch, WorldSnapshot snapshot)
        {
            if (!sequence.Accept(batch, epoch, snapshot == null ? 0 : snapshot.SentAt, Time.realtimeSinceStartup)) { Dropped += batch.Cues.Count; return; }
            replaying = true;
            try {
                foreach (SoundCue cue in batch.Cues) {
                    Source source;
                    if (!receivers.TryGetValue(cue.Emitter, out source)) {
                        if (receivers.Count >= 256) { Dropped++; continue; }
                        source = new Source { Object = new GameObject("GT_NetworkSound") }; receivers.Add(cue.Emitter, source);
                    }
                    source.Touched = Time.realtimeSinceStartup;
                    source.Object.transform.position = new Vector3(cue.X, cue.Y, 0);
                    foreach (SoundSwitch s in cue.Switches) { AkSoundEngine.SetSwitch(s.Group, s.Value, source.Object); SwitchesApplied++; }
                    uint playing = AkSoundEngine.PostEvent(cue.Event, source.Object);
                    Dispatched++;
                    // Smoke intentionally intercepts PostEvent before the audio engine.
                    if (playing == 0 && !DungeonPlugin.Instance.IsSmoke) FailedEvents++;
                }
            } finally { replaying = false; }
            Maintain();
        }
        public void Maintain()
        {
            expiredReceivers.Clear();
            foreach (KeyValuePair<uint, Source> pair in receivers) if (Time.realtimeSinceStartup - pair.Value.Touched > 5) expiredReceivers.Add(pair.Key);
            foreach (uint id in expiredReceivers) { DestroySource(receivers[id]); receivers.Remove(id); }
        }
        private static void DestroySource(Source source)
        {
            if (source.Object == null) return;
            AkSoundEngine.StopAll(source.Object); UnityEngine.Object.Destroy(source.Object);
        }
        public void Dispose()
        {
            foreach (Source source in receivers.Values) DestroySource(source);
            receivers.Clear(); sources.Clear(); pending.Clear();
        }
        [HarmonyPatch]
        private static class Events
        {
            private static IEnumerable<MethodBase> TargetMethods()
            {
                // All twelve managed overloads call native Wwise directly (no nesting).
                foreach (MethodInfo method in typeof(AkSoundEngine).GetMethods(BindingFlags.Static | BindingFlags.Public))
                    if (method.Name == "PostEvent" && method.ReturnType == typeof(uint)) yield return method;
            }
            [HarmonyPriority(Priority.First)]
            private static bool Prefix(object __0, GameObject __1, ref uint __result)
            {
                DungeonSession s = Session;
                if (s == null || s.Phase != DungeonSession.RunPhase.Running || replaying || !BattleEvent(__0)) return true;
                if (s.IsHost) { s.Audio.Capture(__0, __1); return true; }
                __result = 0; return false;
            }
        }
        [HarmonyPatch]
        private static class Switches
        {
            private static IEnumerable<MethodBase> TargetMethods()
            {
                foreach (MethodInfo method in typeof(AkSoundEngine).GetMethods(BindingFlags.Static | BindingFlags.Public))
                    if (method.Name == "SetSwitch" && method.GetParameters().Length == 3 && method.GetParameters()[2].ParameterType == typeof(GameObject)) yield return method;
            }
            private static void Prefix(object __0, object __1, GameObject __2)
            {
                DungeonSession s = Session;
                if (s == null || !s.IsHost || s.Phase != DungeonSession.RunPhase.Running || replaying) return;
                Source source = s.Audio.GetSource(__2); if (source == null) return;
                uint group = Id(__0);
                if (source.Switches.ContainsKey(group) || source.Switches.Count < SoundBatch.MaxSwitches) source.Switches[group] = Id(__1);
            }
        }
    }
}
