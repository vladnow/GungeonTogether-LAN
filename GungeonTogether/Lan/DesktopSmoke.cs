using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

namespace GungeonTogether.Lan
{
    // Only attached by the explicit diagnostic launch flags. Ordinary play keeps
    // its own resolution, focus and cursor behaviour.
    internal sealed class DesktopSmoke : MonoBehaviour
    {
        [DllImport("user32.dll")] private static extern int GetWindowLong(IntPtr window, int index);
        [DllImport("user32.dll")] private static extern int SetWindowLong(IntPtr window, int index, int value);
        [DllImport("user32.dll")] private static extern bool SetWindowPos(IntPtr window, IntPtr after, int x, int y, int width, int height, uint flags);
        [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
        private delegate bool WindowCallback(IntPtr handle, IntPtr parameter);
        [DllImport("user32.dll")] private static extern bool EnumWindows(WindowCallback callback, IntPtr parameter);
        [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr handle, out uint process);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetClassName(IntPtr handle, StringBuilder name, int size);
        [DllImport("kernel32.dll")] private static extern uint GetCurrentProcessId();
        private IntPtr window;
        private float nextCheck;
        public bool Verified { get; private set; }

        private void Awake()
        {
            Application.runInBackground = true;
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
            Screen.SetResolution(800, 600, false);
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
            ConfigureWindow();
        }

        private void ConfigureWindow()
        {
            uint current = GetCurrentProcessId();
            EnumWindows(delegate(IntPtr handle, IntPtr parameter) {
                uint owner; GetWindowThreadProcessId(handle, out owner);
                if (owner != current) return true;
                StringBuilder name = new StringBuilder(128); GetClassName(handle, name, name.Capacity);
                if (name.ToString() != "UnityWndClass") return true;
                window = handle; return false;
            }, IntPtr.Zero);
            if (window == IntPtr.Zero) return;
            // WS_EX_NOACTIVATE: the diagnostic window cannot take focus on click.
            SetWindowLong(window, -20, GetWindowLong(window, -20) | 0x08000000);
            SetWindowPos(window, new IntPtr(1), 20, 20, 0, 0, 0x0010 | 0x0001 | 0x0040);
        }

        private void LateUpdate()
        {
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
            if (Time.realtimeSinceStartup < nextCheck) return;
            nextCheck = Time.realtimeSinceStartup + 1;
            if (GameManager.HasInstance) HarmonyLib.AccessTools.Field(typeof(GameManager), "m_applicationHasFocus").SetValue(GameManager.Instance, true);
            if (window == IntPtr.Zero) ConfigureWindow();
            if (Screen.fullScreen || Screen.width > 820 || Screen.height > 620) Screen.SetResolution(800, 600, false);
            Verified = window != IntPtr.Zero && !Screen.fullScreen && Screen.width <= 820 && Screen.height <= 620 &&
                Cursor.lockState == CursorLockMode.None && GetForegroundWindow() != window;
        }
    }
}
