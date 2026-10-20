using System;
using UnityEngine;

namespace GungeonTogether.Lan
{
    public sealed partial class DungeonPlugin
    {
        private bool hostTab = true, showNetworkDetails;
        private GUIStyle menuTitle, menuText, menuSmall, menuButton, menuField, menuValue;
        private static readonly Color Ink = new Color(0.075f, 0.068f, 0.068f, 0.98f);
        private static readonly Color Panel = new Color(0.13f, 0.115f, 0.105f, 1);
        private static readonly Color Border = new Color(0.38f, 0.32f, 0.23f, 1);
        private static readonly Color Gold = new Color(0.79f, 0.66f, 0.39f, 1);
        private static readonly Color Paper = new Color(0.91f, 0.87f, 0.76f, 1);
        private static readonly Color Muted = new Color(0.61f, 0.59f, 0.53f, 1);

        private void InitMenuStyles()
        {
            if (menuTitle != null) return;
            menuTitle = TextStyle(25, Gold, TextAnchor.MiddleLeft);
            menuText = TextStyle(16, Paper, TextAnchor.UpperLeft);
            menuSmall = TextStyle(13, Muted, TextAnchor.UpperLeft);
            menuValue = TextStyle(19, Paper, TextAnchor.MiddleCenter);
            menuButton = TextStyle(16, Paper, TextAnchor.MiddleCenter);
            menuField = new GUIStyle(GUI.skin.textField);
            menuField.fontSize = 18;
            menuField.padding = new RectOffset(10, 8, 7, 5);
            menuField.normal.textColor = Paper; menuField.focused.textColor = Paper;
            menuField.normal.background = Texture2D.whiteTexture;
            menuField.focused.background = Texture2D.whiteTexture;
        }

        private static GUIStyle TextStyle(int size, Color color, TextAnchor anchor)
        {
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.fontSize = size; style.normal.textColor = color; style.alignment = anchor;
            style.wordWrap = true; style.padding = new RectOffset(0, 0, 0, 0);
            return style;
        }

        private static void Fill(Rect rect, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color; GUI.DrawTexture(rect, Texture2D.whiteTexture); GUI.color = previous;
        }

        private static void Frame(Rect rect, Color fill, Color edge)
        {
            Fill(rect, edge);
            Fill(new Rect(rect.x + 2, rect.y + 2, rect.width - 4, rect.height - 4), fill);
        }

        private bool MenuButton(Rect rect, string text, bool enabled, bool accent)
        {
            bool hover = enabled && rect.Contains(Event.current.mousePosition);
            Color edge = accent ? Gold : Border;
            Color fill = hover ? new Color(0.25f, 0.215f, 0.155f) : Panel;
            Frame(rect, fill, enabled ? edge : new Color(0.22f, 0.205f, 0.18f));
            GUIStyle style = menuButton;
            Color before = style.normal.textColor;
            style.normal.textColor = enabled ? (accent ? Gold : Paper) : Muted;
            style.hover.textColor = style.active.textColor = style.focused.textColor = style.normal.textColor;
            bool previous = GUI.enabled; GUI.enabled = enabled;
            bool clicked = GUI.Button(rect, "", style);
            GUI.enabled = previous;
            GUI.Label(rect, text, style);
            style.normal.textColor = before;
            return clicked;
        }

        private string MenuField(Rect rect, string value, int length, bool enabled)
        {
            Frame(rect, new Color(0.055f, 0.05f, 0.05f), Border);
            Color previous = GUI.backgroundColor; GUI.backgroundColor = new Color(0.055f, 0.05f, 0.05f);
            bool wasEnabled = GUI.enabled; GUI.enabled = enabled;
            string result = GUI.TextField(new Rect(rect.x + 2, rect.y + 2, rect.width - 4, rect.height - 4), value, length, menuField);
            GUI.enabled = wasEnabled; GUI.backgroundColor = previous;
            return result;
        }

        private void OnGUI()
        {
            if (transport == null) return;
            if (Session != null && Session.Mirroring && InventoryBook.BookOpen) InventoryBook.DrawCaption(Session);
            if (IsSmoke && !smokeMenuVisible) return;
            InitMenuStyles();
            if (!visible) return;
            Matrix4x4 previous = GUI.matrix;
            float scale = Mathf.Min(1, Mathf.Min(Screen.width / 692f, Screen.height / 642f));
            scale = Mathf.Max(0.4f, scale);
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1));
            try {
                float width = Screen.width / scale, height = Screen.height / scale;
                window.x = Mathf.Clamp(window.x, 8, Mathf.Max(8, width - window.width - 8));
                window.y = Mathf.Clamp(window.y, 8, Mathf.Max(8, height - window.height - 8));
                window = GUI.Window(27631, window, DrawMenu, "", GUIStyle.none);
            } finally { GUI.matrix = previous; }
        }

        private void DrawMenu(int id)
        {
            // Stepped corners and restrained cartridge colours use no game assets.
            Fill(new Rect(6, 6, 614, 580), new Color(0, 0, 0, 0.5f));
            Frame(new Rect(0, 0, 614, 580), Ink, Border);
            Fill(new Rect(6, 6, 602, 2), Gold);
            Fill(new Rect(26, 26, 12, 23), Gold);
            Fill(new Rect(28, 21, 8, 5), Gold);
            Fill(new Rect(24, 50, 16, 3), Border);
            GUI.Label(new Rect(55, 18, 480, 39), "GUNGEON TOGETHER", menuTitle);
            GUI.Label(new Rect(55, 59, 490, 23), "NETWORK CO-OP   /   LAN 0.3.9 · ALPHA", menuSmall);
            if (MenuButton(new Rect(550, 23, 36, 32), "×", true, false)) visible = false;

            if (!UsesSeparateProfile) {
                GUI.Label(new Rect(28, 113, 558, 130), "Normal saves are active.\n\nFor co-op, use Start Modded in a separate mod profile or launch with the LAN launcher.", menuText);
                GUI.Label(new Rect(28, 480, 558, 44), "Close menu: Esc or Ctrl+P", menuSmall);
                GUI.DragWindow(new Rect(0, 0, 540, 82)); DrawMenuCursor(); return;
            }
            if (transport.Active) hostTab = transport.IsHost;
            if (MenuButton(new Rect(28, 99, 272, 37), "HOST", !transport.Active, hostTab)) hostTab = true;
            if (MenuButton(new Rect(314, 99, 272, 37), "JOIN", !transport.Active, !hostTab)) hostTab = false;

            GUI.Label(new Rect(28, 152, 558, 23), hostTab ? "Listen address" : "Host Radmin / LAN IPv4", menuSmall);
            if (hostTab) bind = MenuField(new Rect(28, 177, 558, 38), bind, 64, !transport.Active);
            else ip = MenuField(new Rect(28, 177, 558, 38), ip, 64, !transport.Active);
            GUI.Label(new Rect(28, 225, 105, 20), "TCP port", menuSmall);
            GUI.Label(new Rect(150, 225, 436, 20), "Room code - use the same code on both PCs", menuSmall);
            port = MenuField(new Rect(28, 250, 105, 38), port, 5, !transport.Active);
            code = MenuField(new Rect(150, 250, 436, 38), code, 64, !transport.Active);

            Frame(new Rect(28, 306, 558, 88), Panel, Border);
            GUI.Label(new Rect(44, 317, 525, 20), "PLAYER TWO · RUN CHARACTER", menuSmall);
            bool canChoose = !hostTab && (Session == null || !Session.InRun);
            int selected = hostTab && Session != null ? Session.GuestCharacter : guestChoice;
            if (hostTab && (Session == null || !Session.GuestChoiceReceived)) {
                GUI.Label(new Rect(44, 348, 525, 32), "Your friend chooses a hero on their PC", menuText);
            } else {
                if (MenuButton(new Rect(44, 344, 42, 34), "<", canChoose, false)) ChangeGuest(-1);
                GUI.Label(new Rect(98, 343, 391, 35), CharacterCatalog.Name(selected), menuValue);
                if (MenuButton(new Rect(528, 344, 42, 34), ">", canChoose, false)) ChangeGuest(1);
            }

            if (!transport.Active) {
                if (MenuButton(new Rect(28, 410, 558, 43), hostTab ? "CREATE ROOM" : "JOIN BY IP", true, true)) Connect(hostTab);
            } else {
                bool start = transport.IsHost && transport.Connected && Session != null && Session.GuestChoiceReceived && !Session.InRun;
                string label = !transport.Connected ? "WAITING FOR CONNECTION…" : Session != null && Session.InRun ? "RUN STARTED" : transport.IsHost ? "START RUN" : "WAITING FOR HOST TO START…";
                if (MenuButton(new Rect(28, 410, 558, 43), label, start, true)) {
                    try { ClearAvatar(); Session.StartHost(); visible = false; error = ""; }
                    catch (Exception ex) { error = ex.Message; Log(error); }
                }
            }
            string link = "Connected · RTT " + transport.RoundTripMilliseconds.ToString("0") + " ms · smoothing " + (Session == null ? 40 : Session.SmoothingMilliseconds).ToString("0") + " ms";
            if (Session != null && Session.Mirroring) link += " · response " + Session.InputLatencyMs.ToString("0") + " ms";
            string status = error.Length != 0 ? error : transport.Connected ? link : transport.Active ? "Check the address and room code on both PCs." : hostTab ? "Share your Radmin / LAN IPv4 and room code with your friend." : "Both players must be on the same LAN or Radmin network.";
            if (error.Length == 0 && Session != null && Session.InRun && Session.Phase != DungeonSession.RunPhase.Running) status = Session.Status;
            if (showNetworkDetails) status = (transport.Connected ? link + " · queue " + transport.PendingBytes / 1024 + " KiB\n" : transport.Status + "\n") + (Session == null ? "" : Session.Status) + (error.Length == 0 ? "" : "\n" + error);
            if (showNetworkDetails && Session != null && Session.MovementBlockReason.Length > 0) status += "\nPlayer two controls: " + Session.MovementBlockReason;
            GUI.Label(new Rect(28, 465, 558, 54), status, menuSmall);
            if (MenuButton(new Rect(28, 534, 248, 30), "DISCONNECT / FOYER", transport.Active || (Session != null && Session.InRun), false)) ReturnToFoyer();
            if (MenuButton(new Rect(290, 534, 142, 30), "DETAILS", true, false)) showNetworkDetails = !showNetworkDetails;
            if (MenuButton(new Rect(446, 534, 140, 30), "CLOSE · Esc", true, false)) visible = false;
            GUI.DragWindow(new Rect(0, 0, 540, 82));
            DrawMenuCursor();
        }

        private void DrawMenuCursor()
        {
            // The game's world-space crosshair is below IMGUI. Paint this last
            // inside the window so buttons and text fields cannot cover it.
            if (Event.current.type != EventType.Repaint) return;
            Vector2 p = Event.current.mousePosition;
            if (!new Rect(0, 0, 614, 580).Contains(p)) return;
            float x = Mathf.Round(p.x), y = Mathf.Round(p.y);
            Color outline = new Color(0.025f, 0.02f, 0.015f);
            Fill(new Rect(x - 7, y - 2, 15, 5), outline);
            Fill(new Rect(x - 2, y - 7, 5, 15), outline);
            Fill(new Rect(x - 6, y - 1, 13, 3), Paper);
            Fill(new Rect(x - 1, y - 6, 3, 13), Paper);
            Fill(new Rect(x - 1, y - 1, 3, 3), Gold);
        }

        private void ChangeGuest(int direction)
        {
            guestChoice = (guestChoice + direction + CharacterCatalog.GuestCount) % CharacterCatalog.GuestCount;
            Config.Bind("LAN", "GuestCharacter", 0, "Guest run character: 0 Cultist, 1 Pilot, 2 Marine, 3 Convict, 4 Hunter.").Value = guestChoice;
            if (Session != null) Session.ChooseGuest(guestChoice);
        }
    }
}
