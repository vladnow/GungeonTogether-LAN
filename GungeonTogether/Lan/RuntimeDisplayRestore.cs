using System;
using System.Collections;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace GungeonTogether.Lan
{
    // A bounded startup refresh for the normal LAN profile. Diagnostic windows
    // and subsequent user resolution changes remain under their existing owners.
    internal sealed class RuntimeDisplayRestore : MonoBehaviour
    {
        private static readonly MethodInfo CheckPixelatorSize = AccessTools.Method(typeof(Pixelator), "CheckSize");
        private static bool scheduled;
        private Action<string> log;

        internal static void Attach(GameObject owner, Action<string> logger)
        {
            if (scheduled || owner == null || !NormalLanStartup()) return;
            scheduled = true;
            RuntimeDisplayRestore recovery = owner.AddComponent<RuntimeDisplayRestore>();
            recovery.log = logger;
        }

        private static bool NormalLanStartup()
        {
            DungeonPlugin plugin = DungeonPlugin.Instance;
            if (plugin == null || plugin.IsSmoke || !plugin.UsesSeparateProfile) return false;
            // Do not apply startup camera sizing after a synchronized run starts.
            return plugin.Session == null || !plugin.Session.InRun;
        }

        private static bool NativeReady()
        {
            if (!GameManager.HasInstance || GameManager.Options == null || Pixelator.Instance == null || GameUIRoot.Instance == null) return false;
            GameManager gm = GameManager.Instance;
            return !gm.IsLoadingLevel && !BraveUtility.isLoadingLevel && !GameManager.IsReturningToBreach &&
                gm.Dungeon != null && gm.MainCameraController != null && gm.MainCameraController.Camera != null &&
                Pixelator.Instance.GetComponent<Camera>() != null && Screen.width > 50 && Screen.height > 50;
        }

        private IEnumerator Start()
        {
            float deadline = Time.realtimeSinceStartup + 30f;
            float stableSince = Time.realtimeSinceStartup;
            int lastWidth = Screen.width, lastHeight = Screen.height, stableFrames = 0;
            bool lastFullscreen = Screen.fullScreen;
            while (Time.realtimeSinceStartup < deadline && NormalLanStartup()) {
                bool ready = NativeReady();
                if (!ready || Screen.width != lastWidth || Screen.height != lastHeight || Screen.fullScreen != lastFullscreen) {
                    lastWidth = Screen.width; lastHeight = Screen.height; lastFullscreen = Screen.fullScreen;
                    stableSince = Time.realtimeSinceStartup; stableFrames = 0;
                } else stableFrames++;
                if (ready && stableFrames >= 4 && Time.realtimeSinceStartup - stableSince >= 0.5f) break;
                yield return null;
            }

            if (!NormalLanStartup()) { Destroy(this); yield break; }
            if (!NativeReady() || Time.realtimeSinceStartup >= deadline) {
                Write("GT_DISPLAY_RESTORE_SKIPPED native startup was not ready within 30 seconds");
                Destroy(this); yield break;
            }

            bool resized = RestoreOnce();
            if (resized) {
                // Unity applies SetResolution at a later frame boundary. Wait a
                // short bounded interval, then refresh the settled actual viewport.
                float settleDeadline = Mathf.Min(deadline, Time.realtimeSinceStartup + 3f);
                stableSince = Time.realtimeSinceStartup; stableFrames = 0;
                lastWidth = Screen.width; lastHeight = Screen.height; lastFullscreen = Screen.fullScreen;
                while (Time.realtimeSinceStartup < settleDeadline && NormalLanStartup()) {
                    if (!NativeReady() || Screen.width != lastWidth || Screen.height != lastHeight || Screen.fullScreen != lastFullscreen) {
                        lastWidth = Screen.width; lastHeight = Screen.height; lastFullscreen = Screen.fullScreen;
                        stableSince = Time.realtimeSinceStartup; stableFrames = 0;
                    } else stableFrames++;
                    if (stableFrames >= 4 && Time.realtimeSinceStartup - stableSince >= 0.25f) break;
                    yield return null;
                }
                if (NormalLanStartup() && NativeReady()) RefreshSafely();
            }
            Destroy(this);
        }

        private bool RestoreOnce()
        {
            try {
                GameOptions options = GameManager.Options;
                int width = options.preferredResolutionX, height = options.preferredResolutionY;
                GameOptions.PreferredFullscreenMode mode = options.CurrentPreferredFullscreenMode;
                if (mode == GameOptions.PreferredFullscreenMode.BORDERLESS) {
                    width = Screen.currentResolution.width; height = Screen.currentResolution.height;
                }
                // Preserve the mode already established by native startup. An
                // unfinished fullscreen/style transition is not ours to replace.
                bool sameMode = Screen.fullScreen == (mode == GameOptions.PreferredFullscreenMode.FULLSCREEN);
                bool validSize = width > 50 && height > 50 && width <= 50000 && height <= 50000;
                bool resize = sameMode && validSize && (width != Screen.width || height != Screen.height);
                Write("GT_DISPLAY_RESTORE screen=" + Screen.width + "x" + Screen.height + " fullscreen=" + Screen.fullScreen +
                    " preferred=" + options.preferredResolutionX + "x" + options.preferredResolutionY + " mode=" + mode + " reapply=" + resize);
                if (resize) GameManager.Instance.DoSetResolution(width, height, Screen.fullScreen);
                RefreshViewport();
                return resize;
            } catch (Exception ex) {
                Write("GT_DISPLAY_RESTORE_SKIPPED " + ex.GetBaseException().Message);
                return false;
            }
        }

        private void RefreshSafely()
        {
            try { RefreshViewport(); }
            catch (Exception ex) { Write("GT_DISPLAY_RESTORE_REFRESH_SKIPPED " + ex.GetBaseException().Message); }
        }

        private void RefreshViewport()
        {
            GameOptions options = GameManager.Options;
            GameOptions.PreferredFullscreenMode chosenMode = options.CurrentPreferredFullscreenMode;
            Camera main = GameManager.Instance.MainCameraController.Camera;
            Rect before = main.rect;
            try {
                Pixelator pixelator = Pixelator.Instance;
                // Use the same native aspect/UI callbacks used by normal rendering;
                // retain safe area and pixel-perfect/uniform scaling preferences.
                BraveCameraUtility.MaintainCameraAspect(main);
                BraveCameraUtility.MaintainCameraAspect(pixelator.GetComponent<Camera>());
                if (pixelator.slavedCameras != null) foreach (Camera camera in pixelator.slavedCameras)
                    if (camera != null) BraveCameraUtility.MaintainCameraAspect(camera);
                if (CheckPixelatorSize != null) CheckPixelatorSize.Invoke(pixelator, null);
                dfGUIManager.ForceResolutionUpdates();
                GameUIRoot.Instance.UpdateScale();
            } finally {
                // Pixelator.CheckSize normalizes this option from Screen.fullScreen.
                // Refreshing a viewport must not overwrite the chosen preference.
                options.CurrentPreferredFullscreenMode = chosenMode;
            }
            Write("GT_DISPLAY_RESTORE_REFRESH screen=" + Screen.width + "x" + Screen.height + " rectBefore=" + before + " rectAfter=" + main.rect);
        }

        private void Write(string message)
        {
            if (log != null) log(message);
        }
    }
}
