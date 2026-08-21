using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using UnityEngine;

[BepInPlugin("org.vinegar.gfr", "Jello", "1.0.0")]
public class QOLMenuPlugin : BasePlugin
{
    public override void Load()
    {
        Log.LogInfo("Do You Even Read The Logs?");
        AddComponent<JelloMenuUI>();
    }
}

public class JelloMenuUI : MonoBehaviour
{
    // =========================
    // MENU
    // =========================

    private bool menuOpen = false;
    private bool waitingForKey = false;

    private KeyCode menuKey = KeyCode.Delete;

    private Rect window =
        new Rect(250, 150, 500, 520);

    private int currentTab = 0;

    private readonly string[] tabs =
    {
        "HUD",
        "Players",
        "UI",
        "Keybinds",
        "Cosmetics",
        "Host Only",
        "Misc",
        "About"
    };

    private GUIStyle? titleStyle;
    private GUIStyle? buttonStyle;
    private GUIStyle? labelStyle;
    private GUIStyle? hudStyle;

    private string statusText =
        "Jello Loaded";

    // =========================
    // UI
    // =========================

    private float menuOpacity = 1.0f;
    private float uiScale = 1.0f;

    private Color backgroundColor =
        new Color(0.05f, 0.05f, 0.07f, 1f);

    private float backgroundTransparency = 0.15f;

    private int theme = 0;

    // =========================
    // HUD
    // =========================

    private bool hudEnabled = true;
    private bool showFPS = true;
    private bool showClock = true;
    private bool showRuntime = false;
    private bool showResolution = false;
    private bool hudEditMode = false;

    // =========================
    // FPS
    // =========================

    private float fps = 0f;
    private float fpsTimer = 0f;
    private int fpsFrames = 0;

    private float fpsWarning = 30f;

    // =========================
    // HUD POSITIONS
    // =========================

    private Rect fpsRect =
        new Rect(15, 15, 180, 30);

    private Rect clockRect =
        new Rect(15, 45, 180, 30);

    private Rect runtimeRect =
        new Rect(15, 75, 220, 30);

    private Rect resolutionRect =
        new Rect(15, 105, 250, 30);

    // =========================
    // PLAYERS
    // =========================

    private bool playersOverlay = false;
    private bool showPlayerColor = true;
    private bool showPlayerStatus = true;
    private bool showPlayerDevice = true;

    private Vector2 playerScroll =
        Vector2.zero;

    private readonly List<PlayerDisplayData>
        players =
        new List<PlayerDisplayData>();

    // =========================
    // COSMETICS
    // =========================

    private bool cosmeticsWarning =
        true;

    // =========================
    // START
    // =========================

    private void Start()
    {
        string savedKey =
            PlayerPrefs.GetString(
                "Jello_MenuKey",
                "Delete"
            );

        if (Enum.TryParse(
            savedKey,
            out KeyCode loadedKey))
        {
            menuKey = loadedKey;
        }

        BuildDemoPlayerList();
    }

    // =========================
    // UPDATE
    // =========================

    private void Update()
    {
        fpsFrames++;
        fpsTimer += Time.unscaledDeltaTime;

        if (fpsTimer >= 0.5f)
        {
            fps = fpsFrames / fpsTimer;

            fpsFrames = 0;
            fpsTimer = 0f;
        }
    }

    // =========================
    // GUI
    // =========================

    private void OnGUI()
    {
        if (!waitingForKey &&
            Event.current != null &&
            Event.current.type == EventType.KeyDown &&
            Event.current.keyCode == menuKey)
        {
            menuOpen = !menuOpen;

            Event.current.Use();
        }

        CreateStyles();

        DrawHUD();

        if (playersOverlay)
            DrawPlayersOverlay();

        if (!menuOpen)
            return;

        Color oldColor = GUI.color;
        Matrix4x4 oldMatrix = GUI.matrix;

        GUI.matrix = Matrix4x4.TRS(
            Vector3.zero,
            Quaternion.identity,
            new Vector3(
                uiScale,
                uiScale,
                1f
            )
        );

        window = GUI.Window(
            12345,
            window,
            (GUI.WindowFunction)(
                (id) => DrawMenu(id)
            ),
            "Jello"
        );

        GUI.matrix = oldMatrix;
        GUI.color = oldColor;
    }

    // =========================
    // MENU
    // =========================

    private void DrawMenu(int id)
    {
        DrawWindowBackground();

        GUILayout.BeginHorizontal();

        // =========================
        // TAB BAR
        // =========================

        GUILayout.BeginVertical(
            GUILayout.Width(115)
        );

        GUILayout.Space(10);

        GUILayout.Label(
            "JELLO",
            titleStyle
        );

        GUILayout.Space(10);

        for (int i = 0; i < tabs.Length; i++)
        {
            GUI.color =
                currentTab == i
                    ? new Color(
                        0.3f,
                        1f,
                        0.5f
                    )
                    : Color.white;

            if (GUILayout.Button(
                tabs[i],
                GUILayout.Height(36)
            ))
            {
                currentTab = i;
                waitingForKey = false;
            }
        }

        GUI.color = Color.white;

        GUILayout.FlexibleSpace();

        if (GUILayout.Button(
            "Close",
            GUILayout.Height(32)
        ))
        {
            menuOpen = false;
            waitingForKey = false;
        }

        GUILayout.Space(10);

        GUILayout.EndVertical();

        // =========================
        // CONTENT
        // =========================

        GUILayout.BeginVertical();

        GUILayout.Space(10);

        switch (currentTab)
        {
            case 0:
                DrawHUDTab();
                break;

            case 1:
                DrawPlayersTab();
                break;

            case 2:
                DrawUITab();
                break;

            case 3:
                DrawKeybindTab();
                break;

            case 4:
                DrawCosmeticsTab();
                break;

            case 5:
                DrawHostTab();
                break;

            case 6:
                DrawMiscTab();
                break;

            case 7:
                DrawAboutTab();
                break;
        }

        GUILayout.FlexibleSpace();

        GUILayout.EndVertical();

        GUILayout.EndHorizontal();

        GUI.DragWindow(
            new Rect(
                0,
                0,
                window.width,
                45
            )
        );
    }

    // =========================
    // HUD TAB
    // =========================

    private void DrawHUDTab()
    {
        GUILayout.Label(
            "HUD",
            titleStyle
        );

        GUILayout.Space(10);

        hudEnabled =
            GUILayout.Toggle(
                hudEnabled,
                "Enable HUD"
            );

        showFPS =
            GUILayout.Toggle(
                showFPS,
                "Show FPS"
            );

        showClock =
            GUILayout.Toggle(
                showClock,
                "Show Clock"
            );

        showRuntime =
            GUILayout.Toggle(
                showRuntime,
                "Show Runtime"
            );

        showResolution =
            GUILayout.Toggle(
                showResolution,
                "Show Resolution"
            );

        hudEditMode =
            GUILayout.Toggle(
                hudEditMode,
                "Edit HUD"
            );

        GUILayout.Space(10);

        GUILayout.Label(
            $"FPS Warning: {fpsWarning:0}",
            labelStyle
        );

        fpsWarning =
            GUILayout.HorizontalSlider(
                fpsWarning,
                10f,
                120f
            );

        GUILayout.Space(10);

        if (GUILayout.Button(
            "Reset HUD",
            GUILayout.Height(35)
        ))
        {
            ResetHUD();

            statusText =
                "HUD reset.";
        }
    }

    // =========================
    // PLAYERS TAB
    // =========================

    private void DrawPlayersTab()
    {
        GUILayout.Label(
            "PLAYERS",
            titleStyle
        );

        GUILayout.Space(8);

        playersOverlay =
            GUILayout.Toggle(
                playersOverlay,
                "Show Player Overlay"
            );

        showPlayerColor =
            GUILayout.Toggle(
                showPlayerColor,
                "Show Color"
            );

        showPlayerStatus =
            GUILayout.Toggle(
                showPlayerStatus,
                "Show Status"
            );

        showPlayerDevice =
            GUILayout.Toggle(
                showPlayerDevice,
                "Show Device"
            );

        GUILayout.Space(10);

        if (GUILayout.Button(
            "Refresh Players",
            GUILayout.Height(32)
        ))
        {
            RefreshPlayers();

            statusText =
                "Player list refreshed.";
        }

        GUILayout.Space(8);

        playerScroll =
            GUILayout.BeginScrollView(
                playerScroll,
                GUILayout.Height(220)
            );

        foreach (PlayerDisplayData player
                 in players)
        {
            GUILayout.BeginVertical(
                GUI.skin.box
            );

            GUILayout.Label(
                player.Name,
                labelStyle
            );

            if (showPlayerColor)
            {
                GUILayout.Label(
                    "Color: " + player.Color,
                    labelStyle
                );
            }

            if (showPlayerStatus)
            {
                GUILayout.Label(
                    "Status: " + player.Status,
                    labelStyle
                );
            }

            if (showPlayerDevice)
            {
                GUILayout.Label(
                    "Device: " + player.Device,
                    labelStyle
                );
            }

            GUILayout.EndVertical();

            GUILayout.Space(4);
        }

        GUILayout.EndScrollView();

        GUILayout.Space(5);

        GUILayout.Label(
            "Player data shown here is limited to information exposed to the client.",
            labelStyle
        );
    }

    // =========================
    // PLAYER OVERLAY
    // =========================

    private void DrawPlayersOverlay()
    {
        Rect overlay =
            new Rect(
                15,
                145,
                250,
                260
            );

        GUI.Box(
            overlay,
            "Players"
        );

        GUILayout.BeginArea(
            new Rect(
                overlay.x + 10,
                overlay.y + 30,
                overlay.width - 20,
                overlay.height - 40
            )
        );

        foreach (PlayerDisplayData player
                 in players)
        {
            string text =
                player.Name;

            if (showPlayerColor)
            {
                text +=
                    " | " +
                    player.Color;
            }

            if (showPlayerStatus)
            {
                text +=
                    " | " +
                    player.Status;
            }

            GUILayout.Label(
                text,
                labelStyle
            );
        }

        GUILayout.EndArea();
    }

    // =========================
    // UI TAB
    // =========================

    private void DrawUITab()
    {
        GUILayout.Label(
            "UI",
            titleStyle
        );

        GUILayout.Space(10);

        GUILayout.Label(
            $"Menu Opacity: {menuOpacity:0.00}",
            labelStyle
        );

        menuOpacity =
            GUILayout.HorizontalSlider(
                menuOpacity,
                0f,
                1f
            );

        GUILayout.Label(
            $"Background Transparency: {backgroundTransparency:0.00}",
            labelStyle
        );

        backgroundTransparency =
            GUILayout.HorizontalSlider(
                backgroundTransparency,
                0f,
                1f
            );

        GUILayout.Label(
            "0 = opaque, 1 = invisible",
            labelStyle
        );

        GUILayout.Space(8);

        GUILayout.Label(
            $"UI Scale: {uiScale:0.00}",
            labelStyle
        );

        uiScale =
            GUILayout.HorizontalSlider(
                uiScale,
                0.75f,
                1.5f
            );

        GUILayout.Space(10);

        if (GUILayout.Button(
            "Black Background",
            GUILayout.Height(30)
        ))
        {
            backgroundColor =
                Color.black;
        }

        if (GUILayout.Button(
            "Jello Green",
            GUILayout.Height(30)
        ))
        {
            backgroundColor =
                new Color(
                    0.05f,
                    0.5f,
                    0.2f,
                    1f
                );
        }

        if (GUILayout.Button(
            "Cyan",
            GUILayout.Height(30)
        ))
        {
            backgroundColor =
                Color.cyan;
        }

        if (GUILayout.Button(
            "Purple",
            GUILayout.Height(30)
        ))
        {
            backgroundColor =
                new Color(
                    0.45f,
                    0.1f,
                    0.7f,
                    1f
                );
        }
    }

    // =========================
    // KEYBINDS
    // =========================

    private void DrawKeybindTab()
    {
        GUILayout.Label(
            "KEYBINDS",
            titleStyle
        );

        GUILayout.Space(10);

        GUILayout.Label(
            "Menu Key: " + menuKey,
            labelStyle
        );

        if (!waitingForKey)
        {
            if (GUILayout.Button(
                "Change Keybind",
                GUILayout.Height(40)
            ))
            {
                waitingForKey = true;
                statusText =
                    "Press a key...";
            }
        }
        else
        {
            GUILayout.Label(
                "Press any key...",
                labelStyle
            );

            if (Event.current != null &&
                Event.current.type ==
                EventType.KeyDown)
            {
                KeyCode newKey =
                    Event.current.keyCode;

                if (newKey != KeyCode.None)
                {
                    menuKey = newKey;

                    PlayerPrefs.SetString(
                        "Jello_MenuKey",
                        menuKey.ToString()
                    );

                    PlayerPrefs.Save();

                    waitingForKey = false;

                    statusText =
                        "Keybind changed.";
                }
            }
        }

        if (GUILayout.Button(
            "Reset Keybind",
            GUILayout.Height(35)
        ))
        {
            menuKey = KeyCode.Delete;

            PlayerPrefs.SetString(
                "Jello_MenuKey",
                "Delete"
            );

            PlayerPrefs.Save();

            waitingForKey = false;

            statusText =
                "Keybind reset.";
        }
    }

    // =========================
    // COSMETICS
    // =========================

    private void DrawCosmeticsTab()
    {
        GUILayout.Label(
            "COSMETICS",
            titleStyle
        );

        GUILayout.Space(10);

        if (cosmeticsWarning)
        {
            GUILayout.BeginVertical(
                GUI.skin.box
            );

            GUILayout.Label(
                "Cosmetic preview/equipment is limited to cosmetics your installation legitimately has access to.",
                labelStyle
            );

            if (GUILayout.Button(
                "Dismiss",
                GUILayout.Height(28)
            ))
            {
                cosmeticsWarning = false;
            }

            GUILayout.EndVertical();
        }

        GUILayout.Space(10);

        GUILayout.Label(
            "Cosmetic browser",
            labelStyle
        );

        GUILayout.Label(
            "Hats",
            labelStyle
        );

        GUILayout.Label(
            "Skins",
            labelStyle
        );

        GUILayout.Label(
            "Pets",
            labelStyle
        );

        GUILayout.Label(
            "Visors",
            labelStyle
        );

        GUILayout.Label(
            "Nameplates",
            labelStyle
        );
    }

    // =========================
    // HOST TAB
    // =========================

    private void DrawHostTab()
    {
        GUILayout.Label(
            "HOST ONLY",
            titleStyle
        );

        GUILayout.Space(10);

        GUILayout.BeginVertical(
            GUI.skin.box
        );

        GUILayout.Label(
            "Host controls",
            labelStyle
        );

        GUILayout.Label(
            "Game-state controls should use the game's supported networking APIs.",
            labelStyle
        );

        GUILayout.EndVertical();

        GUILayout.Space(10);

        if (GUILayout.Button(
            "Player Management",
            GUILayout.Height(35)
        ))
        {
            statusText =
                "Host player management.";
        }

        if (GUILayout.Button(
            "Commands",
            GUILayout.Height(35)
        ))
        {
            statusText =
                "Host commands selected.";
        }
    }

    // =========================
    // MISC
    // =========================

    private void DrawMiscTab()
    {
        GUILayout.Label(
            "MISC",
            titleStyle
        );

        GUILayout.Space(10);

        GUILayout.Label(
            statusText,
            labelStyle
        );

        if (GUILayout.Button(
            "Test Button",
            GUILayout.Height(40)
        ))
        {
            statusText =
                "Button works!";

            Debug.Log(
                "[Jello] Test Button Works!"
            );
        }

        if (GUILayout.Button(
            "Reset Everything",
            GUILayout.Height(35)
        ))
        {
            ResetHUD();

            menuOpacity = 1f;
            uiScale = 1f;
            backgroundTransparency = 0.15f;

            statusText =
                "Jello settings reset.";
        }
    }

    // =========================
    // ABOUT
    // =========================

    private void DrawAboutTab()
    {
        GUILayout.Label(
            "ABOUT",
            titleStyle
        );

        GUILayout.Space(15);

        GUILayout.Label(
            "Jello",
            labelStyle
        );

        GUILayout.Label(
            "Version 1.0.0",
            labelStyle
        );

        GUILayout.Space(10);

        GUILayout.Label(
            "QOL menu for Among Us.",
            labelStyle
        );

        GUILayout.Label(
            statusText,
            labelStyle
        );
    }

    // =========================
    // WINDOW BACKGROUND
    // =========================

    private void DrawWindowBackground()
    {
        Color oldColor =
            GUI.color;

        Color color =
            backgroundColor;

        color.a =
            1f - backgroundTransparency;

        GUI.color =
            color;

        GUI.Box(
            new Rect(
                0,
                0,
                window.width,
                window.height
            ),
            GUIContent.none
        );

        GUI.color =
            oldColor;
    }

    // =========================
    // HUD
    // =========================

    private void DrawHUD()
    {
        if (!hudEnabled)
            return;

        if (showFPS)
        {
            GUI.color =
                GetFPSColor();

            GUI.Label(
                fpsRect,
                $"FPS: {fps:0}",
                hudStyle
            );

            GUI.color =
                Color.white;

            if (hudEditMode)
                GUI.Box(
                    fpsRect,
                    ""
                );
        }

        if (showClock)
        {
            GUI.Label(
                clockRect,
                DateTime.Now.ToString(
                    "HH:mm:ss"
                ),
                hudStyle
            );
        }

        if (showRuntime)
        {
            TimeSpan runtime =
                TimeSpan.FromSeconds(
                    Time.realtimeSinceStartup
                );

            GUI.Label(
                runtimeRect,
                $"Runtime: {runtime:hh\\:mm\\:ss}",
                hudStyle
            );
        }

        if (showResolution)
        {
            GUI.Label(
                resolutionRect,
                $"Resolution: {Screen.width}x{Screen.height}",
                hudStyle
            );
        }

        if (hudEditMode &&
            Event.current != null &&
            Event.current.type ==
            EventType.MouseDrag &&
            Event.current.button == 0)
        {
            Vector2 mouse =
                Event.current.mousePosition;

            if (fpsRect.Contains(mouse))
            {
                fpsRect.position +=
                    Event.current.delta;
            }
            else if (clockRect.Contains(mouse))
            {
                clockRect.position +=
                    Event.current.delta;
            }
            else if (runtimeRect.Contains(mouse))
            {
                runtimeRect.position +=
                    Event.current.delta;
            }
            else if (resolutionRect.Contains(mouse))
            {
                resolutionRect.position +=
                    Event.current.delta;
            }
        }
    }

    // =========================
    // PLAYER DATA
    // =========================

    private void BuildDemoPlayerList()
    {
        players.Clear();

        players.Add(
            new PlayerDisplayData(
                "Player",
                "Unknown",
                "Unknown",
                "Unknown"
            )
        );
    }

    private void RefreshPlayers()
    {
        // Intentionally API-independent for now.
        //
        // Once we identify the exact PlayerControl/
        // GameData API in your Among Us build, this
        // method can populate the real player list.
        BuildDemoPlayerList();
    }

    // =========================
    // RESET
    // =========================

    private void ResetHUD()
    {
        fpsRect =
            new Rect(
                15,
                15,
                180,
                30
            );

        clockRect =
            new Rect(
                15,
                45,
                180,
                30
            );

        runtimeRect =
            new Rect(
                15,
                75,
                220,
                30
            );

        resolutionRect =
            new Rect(
                15,
                105,
                250,
                30
            );
    }

    // =========================
    // FPS COLOR
    // =========================

    private Color GetFPSColor()
    {
        if (fps >= fpsWarning)
            return Color.green;

        if (fps >= fpsWarning * 0.66f)
            return Color.yellow;

        return Color.red;
    }

    // =========================
    // STYLES
    // =========================

    private void CreateStyles()
    {
        if (titleStyle != null)
            return;

        titleStyle =
            new GUIStyle(
                GUI.skin.label
            )
            {
                fontSize = 20,
                fontStyle =
                    FontStyle.Bold,
                alignment =
                    TextAnchor.MiddleCenter
            };

        labelStyle =
            new GUIStyle(
                GUI.skin.label
            )
            {
                fontSize = 14,
                alignment =
                    TextAnchor.MiddleCenter
            };

        hudStyle =
            new GUIStyle(
                GUI.skin.label
            )
            {
                fontSize = 16,
                fontStyle =
                    FontStyle.Bold
            };

        buttonStyle =
            new GUIStyle(
                GUI.skin.button
            )
            {
                fontSize = 14
            };
    }

    // =========================
    // PLAYER DISPLAY DATA
    // =========================

    private class PlayerDisplayData
    {
        public string Name;
        public string Color;
        public string Device;
        public string Status;

        public PlayerDisplayData(
            string name,
            string color,
            string device,
            string status)
        {
            Name = name;
            Color = color;
            Device = device;
            Status = status;
        }
    }
}
