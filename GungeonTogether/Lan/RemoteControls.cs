using System;
using System.Reflection;
using InControl;
using UnityEngine;

namespace GungeonTogether.Lan
{
    public sealed class RemoteControls : IDisposable
    {
        private sealed class NetworkDevice : InputDevice
        {
            private readonly RemoteControls owner;
            public NetworkDevice(RemoteControls owner) : base("GungeonTogether remote player")
            {
                this.owner = owner; Passive = true;
                for (int i = 0; i < 41; i++) if (!HasControl(ControlFor(i))) AddControl(ControlFor(i), "Network action " + i);
            }
            public override void Update(ulong tick, float deltaTime)
            {
                RemoteInput input = owner.state;
                bool stale = Time.realtimeSinceStartup - owner.lastReceived > 0.25f || !owner.Enabled;
                for (int i = 0; i < 41; i++) {
                    float value = 0;
                    if (!stale) {
                        switch (i) {
                            case 0: value = Mathf.Max(0, -input.MoveX); break;
                            case 1: value = Mathf.Max(0, input.MoveX); break;
                            case 2: value = Mathf.Max(0, input.MoveY); break;
                            case 3: value = Mathf.Max(0, -input.MoveY); break;
                            case 4: value = Mathf.Max(0, -input.AimX); break;
                            case 5: value = Mathf.Max(0, input.AimX); break;
                            case 6: value = Mathf.Max(0, input.AimY); break;
                            case 7: value = Mathf.Max(0, -input.AimY); break;
                            default: value = (input.Buttons & (1UL << i)) != 0 ? 1 : 0; break;
                        }
                    }
                    UpdateWithValue(ControlFor(i), value, tick, deltaTime);
                }
                base.Update(tick, deltaTime);
            }
        }

        private static readonly FieldInfo IdField = typeof(BraveInput).GetField("m_playerID", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo ActionsField = typeof(BraveInput).GetField("m_activeGungeonActions", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly MethodInfo AttachActions = typeof(InputManager).GetMethod("AttachPlayerActionSet", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
        private readonly NetworkDevice device;
        private readonly GungeonActions actions;
        private readonly bool previousSuspend;
        private GungeonActions previousActions;
        private BraveInput target;
        private RemoteInput state = new RemoteInput();
        private float lastReceived;
        public bool Enabled;
        public InputDevice Device { get { return device; } }
        public GungeonActions Actions { get { return actions; } }
        public uint LastInputSequence { get { return state.Sequence; } }
        public float LastMoveX { get { return state.MoveX; } }

        public RemoteControls()
        {
            if (!InputManager.IsSetup) throw new InvalidOperationException("Game input is not ready yet; wait in the foyer.");
            previousSuspend = InputManager.SuspendInBackground;
            SetSuspend(false);
            device = new NetworkDevice(this);
            InputManager.AttachDevice(device);
            actions = new GungeonActions();
            foreach (PlayerAction action in actions.Actions) action.ClearBindings();
            for (int i = 0; i < 41; i++) {
                PlayerAction action = actions.GetActionFromType((GungeonActions.GungeonActionType)i);
                if (action != null) action.AddBinding(new DeviceBindingSource(ControlFor(i)));
            }
            actions.Device = device; actions.ForceDisable = false; actions.Enabled = true;
            actions.LastInputType = BindingSourceType.DeviceBindingSource;
        }

        private static InputControlType ControlFor(int action)
        {
            InputControlType[] axes = { InputControlType.LeftStickLeft, InputControlType.LeftStickRight, InputControlType.LeftStickUp, InputControlType.LeftStickDown, InputControlType.RightStickLeft, InputControlType.RightStickRight, InputControlType.RightStickUp, InputControlType.RightStickDown };
            if (action < 8) return axes[action];
            if (action < 20) return (InputControlType)((int)InputControlType.Action1 + action - 8);
            if (action < 40) return (InputControlType)((int)InputControlType.Button0 + action - 20);
            return InputControlType.Back;
        }

        public static bool IsPlayerTwo(BraveInput input) { return input != null && IdField != null && (int)IdField.GetValue(input) == 1; }
        public void Bind()
        {
            if (InputManager.SuspendInBackground) SetSuspend(false);
            if (!InputManager.Devices.Contains(device)) InputManager.AttachDevice(device);
            AttachActions.Invoke(null, new object[] { actions });
            BraveInput input = BraveInput.GetInstanceForPlayer(1);
            if (input == null) return;
            if (input != target) { target = input; previousActions = (GungeonActions)ActionsField.GetValue(input); }
            ActionsField.SetValue(input, actions);
            actions.Device = device; actions.ForceDisable = false; actions.Enabled = true;
        }
        public void Apply(RemoteInput input)
        {
            if (!Wire.Newer(input.Sequence, state.Sequence)) return;
            state = input; lastReceived = Time.realtimeSinceStartup;
        }
        public void Reset() { state = new RemoteInput(); lastReceived = 0; Enabled = false; }
        public void Dispose()
        {
            Enabled = false;
            if (target != null && previousActions != null) ActionsField.SetValue(target, previousActions);
            actions.Destroy(); InputManager.DetachDevice(device);
            SetSuspend(previousSuspend);
        }
        private static void SetSuspend(bool value)
        { typeof(InputManager).GetProperty("SuspendInBackground", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).GetSetMethod(true).Invoke(null, new object[] { value }); }
    }
}
