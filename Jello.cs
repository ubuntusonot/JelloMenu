using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using UnityEngine;

[BepInPlugin("org.vinegar.gfr", "Jello", "1.1.0")]
public class QOLMenuPlugin : BasePlugin
{
    public override void Load()
    {
        Log.LogInfo("[Jello] Loading Jello 1.1.0");
        AddComponent<JelloMenuUI>();
    }
}

public class JelloMenuUI : MonoBehaviour
{
    private bool menuOpen;
    private bool waitingForKey;
    private KeyCode menuKey = KeyCode.Delete;

    private Rect window = new Rect(250, 120, 650, 560);

    private int currentTab;
    private int previousTab;

    private readonly string[] tabs =
    {
        "HUD", "Players", "UI", "Keybinds",
        "Cosmetics", "Host Only", "Misc", "About"
    };

    private GUIStyle titleStyle = null!;
    private GUIStyle labelStyle = null!;
    private GUIStyle hudStyle = null!;
    private GUIStyle smallStyle = null!;
    private GUIStyle boxStyle = null!;

    private string statusText = "Jello Loaded";

    private readonly List<string> notifications =
        new List<string>();

    private float notificationTimer;

    private bool animationsEnabled = true;
    private float animationSpeed = 8f;
    private float menuAnimation;
    private float targetMenuAnimation;
    private float tabAnimation = 1f;

    private readonly float[] tabHover = new float[8];

    private float menuOpacity = 1f;
    private float uiScale = 1f;

    private Color backgroundColor =
        new Color(0.04f, 0.045f, 0.06f, 1f);

    private Color accentColor =
        new Color(0.25f, 1f, 0.5f, 1f);

    private float backgroundTransparency;
    private int theme;

    // HUD
    private bool hudEnabled = true;
    private bool showFPS = true;
    private bool showClock = true;
    private bool showRuntime;
    private bool showResolution;
    private bool showFPSGraph;
    private bool hudEditMode;
    private bool showStatus;

    private Color hudColor = Color.white;

    private Rect fpsRect = new Rect(15, 15, 180, 30);
    private Rect clockRect = new Rect(15, 45, 180, 30);
    private Rect runtimeRect = new Rect(15, 75, 220, 30);
    private Rect resolutionRect = new Rect(15, 105, 250, 30);
    private Rect graphRect = new Rect(15, 140, 220, 70);

    private float fpsWarning = 30f;

    // FPS
    private float fps;
    private float fpsTimer;
    private int fpsFrames;

    private readonly float[] fpsHistory = new float[60];
    private int fpsHistoryIndex;

    // Players
    private bool playersOverlay;
    private bool showPlayerColor = true;
    private bool showPlayerStatus = true;
    private bool showPlayerDevice = true;
    private bool showIdleTime = true;
    private bool sortPlayers;

    private string playerSearch = "";
    private Vector2 playerScroll;

    private readonly List<PlayerDisplayData> players =
        new List<PlayerDisplayData>();

    private float playerRefreshTimer;

    // Cosmetics
    private bool cosmeticsWarning = true;
    private int cosmeticCategory;

    private readonly string[] cosmeticCategories =
    {
        "Hats",
        "Skins",
        "Pets",
        "Visors",
        "Nameplates"
    };

    private readonly string[] cosmeticItems =
    {
        "Default",
        "Classic",
        "Blue",
        "Red",
        "Green",
        "Purple",
        "Gold",
        "Shadow",
        "Explorer",
        "Astronaut"
    };

    // Host
    private int hostSection;

    private readonly string[] hostSections =
    {
        "Overview",
        "Players",
        "Lobby",
        "Presets"
    };

    private bool hostConfirmation;
    private string pendingHostAction = "";

    // Misc
    private bool debugInfo;
    private bool notificationsEnabled = true;
    private bool compactMode;

    private int selectedPreset;

    private readonly string[] presetNames =
    {
        "Default",
        "Minimal",
        "Performance",
        "Streamer"
    };

    private void Start()
    {
        LoadSettings();
        BuildDemoPlayerList();

        AddNotification("Jello loaded successfully.");
    }

    private void Update()
    {
        UpdateFPS();
        UpdateAnimation();
        UpdatePlayers();

        if (notificationTimer > 0f)
            notificationTimer -= Time.unscaledDeltaTime;
    }

    private void UpdateFPS()
    {
        fpsFrames++;
        fpsTimer += Time.unscaledDeltaTime;

        if (fpsTimer >= 0.5f)
        {
            fps = fpsFrames / fpsTimer;

            fpsFrames = 0;
            fpsTimer = 0f;

            fpsHistory[fpsHistoryIndex] = fps;

            fpsHistoryIndex =
                (fpsHistoryIndex + 1) %
                fpsHistory.Length;
        }
    }

    private void UpdateAnimation()
    {
        targetMenuAnimation = menuOpen ? 1f : 0f;

        if (!animationsEnabled)
        {
            menuAnimation = targetMenuAnimation;
            tabAnimation = 1f;
            return;
        }

        menuAnimation = Mathf.MoveTowards(
            menuAnimation,
            targetMenuAnimation,
            Time.unscaledDeltaTime * animationSpeed
        );

        tabAnimation = Mathf.MoveTowards(
            tabAnimation,
            1f,
            Time.unscaledDeltaTime * animationSpeed
        );
    }

    private void OnGUI()
    {
        CreateStyles();
        HandleMenuKey();

        DrawHUD();

        if (playersOverlay)
            DrawPlayersOverlay();

        DrawNotifications();

        if (menuAnimation <= 0.001f)
            return;

        DrawAnimatedMenu();
    }

    private void HandleMenuKey()
    {
        if (waitingForKey)
            return;

        if (Event.current == null)
            return;

        if (Event.current.type != EventType.KeyDown)
            return;

        if (Event.current.keyCode != menuKey)
            return;

        menuOpen = !menuOpen;
        Event.current.Use();
    }

    private void DrawAnimatedMenu()
    {
        Color oldColor = GUI.color;
        Matrix4x4 oldMatrix = GUI.matrix;

        float eased = EaseOutCubic(menuAnimation);

        GUI.color = new Color(
            1f,
            1f,
            1f,
            eased * menuOpacity
        );

        float scale =
            Mathf.Lerp(0.90f, 1f, eased) * uiScale;

        Vector2 center = new Vector2(
            window.x + window.width / 2f,
            window.y + window.height / 2f
        );

        GUI.matrix =
            Matrix4x4.TRS(
                center,
                Quaternion.identity,
                new Vector3(scale, scale, 1f)
            ) *
            Matrix4x4.TRS(
                -center,
                Quaternion.identity,
                Vector3.one
            );

        window = GUI.Window(
            1337,
            window,
            new GUI.WindowFunction(DrawMenu),
            "Jello"
        );

        GUI.matrix = oldMatrix;
        GUI.color = oldColor;
    }

    private void DrawMenu(int id)
    {
        DrawWindowBackground();

        GUILayout.BeginHorizontal();

        DrawTabs();

        GUILayout.BeginVertical();

        GUILayout.Space(12);

        Color oldColor = GUI.color;

        GUI.color = new Color(
            1f,
            1f,
            1f,
            animationsEnabled
                ? EaseOutCubic(tabAnimation)
                : 1f
        );

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

        GUI.color = oldColor;

        GUILayout.FlexibleSpace();

        GUILayout.EndVertical();
        GUILayout.EndHorizontal();

        GUI.DragWindow(
            new Rect(
                0,
                0,
                window.width,
                42
            )
        );
    }

    private void DrawTabs()
    {
        GUILayout.BeginVertical(
            GUILayout.Width(
                compactMode ? 95 : 125
            )
        );

        GUILayout.Space(12);

        GUILayout.Label("JELLO", titleStyle);

        GUILayout.Space(10);

        for (int i = 0; i < tabs.Length; i++)
        {
            Rect rect = GUILayoutUtility.GetRect(
                new GUIContent(tabs[i]),
                GUI.skin.button,
                GUILayout.Height(38)
            );

            bool hover =
                rect.Contains(Event.current.mousePosition);

            tabHover[i] = Mathf.MoveTowards(
                tabHover[i],
                hover ? 1f : 0f,
                Time.unscaledDeltaTime * animationSpeed
            );

            Color old = GUI.color;

            if (currentTab == i)
            {
                GUI.color = Color.Lerp(
                    accentColor * 0.65f,
                    accentColor,
                    tabHover[i]
                );
            }
            else if (hover)
            {
                GUI.color = new Color(
                    0.8f,
                    1f,
                    0.85f
                );
            }

            if (GUI.Button(rect, tabs[i]))
                SelectTab(i);

            GUI.color = old;
        }

        GUILayout.FlexibleSpace();

        if (GUILayout.Button(
            "Close",
            GUILayout.Height(36)
        ))
        {
            menuOpen = false;
            waitingForKey = false;
        }

        GUILayout.Space(10);

        GUILayout.EndVertical();
    }

    private void SelectTab(int index)
    {
        if (currentTab == index)
            return;

        previousTab = currentTab;
        currentTab = index;
        tabAnimation = 0f;
        waitingForKey = false;
    }

    // =========================================================
    // HUD
    // =========================================================

    private void DrawHUDTab()
    {
        GUILayout.Label("HUD", titleStyle);
        GUILayout.Space(8);

        hudEnabled = GUILayout.Toggle(
            hudEnabled,
            "Enable HUD"
        );

        showFPS = GUILayout.Toggle(showFPS, "FPS");
        showClock = GUILayout.Toggle(showClock, "Clock");
        showRuntime = GUILayout.Toggle(showRuntime, "Runtime");
        showResolution = GUILayout.Toggle(showResolution, "Resolution");
        showFPSGraph = GUILayout.Toggle(showFPSGraph, "FPS Graph");
        showStatus = GUILayout.Toggle(showStatus, "Status");
        hudEditMode = GUILayout.Toggle(hudEditMode, "HUD Edit Mode");

        GUILayout.Space(8);

        GUILayout.Label(
            $"FPS threshold: {fpsWarning:0}",
            labelStyle
        );

        fpsWarning = GUILayout.HorizontalSlider(
            fpsWarning,
            10f,
            120f
        );

        GUILayout.Space(8);

        if (GUILayout.Button(
            "Move HUD Left",
            GUILayout.Height(30)
        ))
            MoveHUD(-10f, 0f);

        if (GUILayout.Button(
            "Move HUD Right",
            GUILayout.Height(30)
        ))
            MoveHUD(10f, 0f);

        if (GUILayout.Button(
            "Reset HUD",
            GUILayout.Height(30)
        ))
        {
            ResetHUD();
            AddNotification("HUD reset.");
        }
    }

    // =========================================================
    // PLAYERS
    // =========================================================

    private void DrawPlayersTab()
    {
        GUILayout.Label("PLAYERS", titleStyle);
        GUILayout.Space(6);

        playersOverlay =
            GUILayout.Toggle(playersOverlay, "Player Overlay");

        showPlayerColor =
            GUILayout.Toggle(showPlayerColor, "Color");

        showPlayerStatus =
            GUILayout.Toggle(showPlayerStatus, "Status");

        showPlayerDevice =
            GUILayout.Toggle(showPlayerDevice, "Device");

        showIdleTime =
            GUILayout.Toggle(showIdleTime, "Idle Time");

        sortPlayers =
            GUILayout.Toggle(sortPlayers, "Sort Alphabetically");

        GUILayout.Label("Search", labelStyle);

        playerSearch =
            GUILayout.TextField(playerSearch);

        GUILayout.Space(5);

        if (GUILayout.Button(
            "Refresh",
            GUILayout.Height(32)
        ))
        {
            RefreshPlayers();
            AddNotification("Players refreshed.");
        }

        playerScroll =
            GUILayout.BeginScrollView(
                playerScroll,
                GUILayout.Height(245)
            );

        for (int i = 0; i < players.Count; i++)
        {
            PlayerDisplayData player = players[i];

            if (!string.IsNullOrEmpty(playerSearch) &&
                !player.Name.ToLower().Contains(
                    playerSearch.ToLower()
                ))
                continue;

            DrawPlayerCard(player);
        }

        GUILayout.EndScrollView();

        GUILayout.Label(
            "Player information is limited to data exposed by the client.",
            smallStyle
        );
    }

    private void DrawPlayerCard(PlayerDisplayData player)
    {
        GUILayout.BeginVertical(GUI.skin.box);

        GUILayout.Label(player.Name, labelStyle);

        if (showPlayerColor)
            GUILayout.Label(
                "Color: " + player.Color,
                smallStyle
            );

        if (showPlayerStatus)
            GUILayout.Label(
                "Status: " + player.Status,
                smallStyle
            );

        if (showPlayerDevice)
            GUILayout.Label(
                "Device: " + player.Device,
                smallStyle
            );

        if (showIdleTime)
            GUILayout.Label(
                "Idle: " + FormatTime(player.IdleTime),
                smallStyle
            );

        GUILayout.EndVertical();

        GUILayout.Space(4);
    }

    // =========================================================
    // UI
    // =========================================================

    private void DrawUITab()
    {
        GUILayout.Label("UI", titleStyle);
        GUILayout.Space(8);

        animationsEnabled =
            GUILayout.Toggle(
                animationsEnabled,
                "Animations"
            );

        GUILayout.Label(
            $"Animation speed: {animationSpeed:0.0}",
            labelStyle
        );

        animationSpeed =
            GUILayout.HorizontalSlider(
                animationSpeed,
                1f,
                20f
            );

        GUILayout.Label(
            $"Menu opacity: {menuOpacity:0.00}",
            labelStyle
        );

        menuOpacity =
            GUILayout.HorizontalSlider(
                menuOpacity,
                0f,
                1f
            );

        GUILayout.Label(
            $"Background transparency: {backgroundTransparency:0.00}",
            labelStyle
        );

        backgroundTransparency =
            GUILayout.HorizontalSlider(
                backgroundTransparency,
                0f,
                1f
            );

        GUILayout.Label(
            $"UI scale: {uiScale:0.00}",
            labelStyle
        );

        uiScale =
            GUILayout.HorizontalSlider(
                uiScale,
                0.75f,
                1.5f
            );

        GUILayout.Space(8);

        GUILayout.Label("Themes", labelStyle);

        if (GUILayout.Button("Jello Green"))
            ApplyTheme(0);

        if (GUILayout.Button("Cyan"))
            ApplyTheme(1);

        if (GUILayout.Button("Purple"))
            ApplyTheme(2);

        if (GUILayout.Button("Red"))
            ApplyTheme(3);

        if (GUILayout.Button("Dark"))
            ApplyTheme(4);

        compactMode =
            GUILayout.Toggle(
                compactMode,
                "Compact Menu"
            );
    }

    // =========================================================
    // KEYBINDS
    // =========================================================

    private void DrawKeybindTab()
    {
        GUILayout.Label("KEYBINDS", titleStyle);
        GUILayout.Space(10);

        GUILayout.Label(
            "Menu key: " + menuKey,
            labelStyle
        );

        if (!waitingForKey)
        {
            if (GUILayout.Button(
                "Change Menu Key",
                GUILayout.Height(40)
            ))
            {
                waitingForKey = true;
                statusText = "Press a key...";
            }
        }
        else
        {
            GUILayout.Label(
                "Press any key...",
                labelStyle
            );

            if (Event.current != null &&
                Event.current.type == EventType.KeyDown)
            {
                if (Event.current.keyCode != KeyCode.None)
                {
                    menuKey = Event.current.keyCode;
                    waitingForKey = false;

                    SaveSettings();
                    AddNotification("Keybind saved.");

                    Event.current.Use();
                }
            }
        }

        GUILayout.Space(8);

        if (GUILayout.Button("Reset Keybind"))
        {
            menuKey = KeyCode.Delete;
            waitingForKey = false;

            SaveSettings();
            AddNotification("Keybind reset.");
        }
    }

    // =========================================================
    // COSMETICS
    // =========================================================

    private void DrawCosmeticsTab()
    {
        GUILayout.Label("COSMETICS", titleStyle);
        GUILayout.Space(8);

        if (cosmeticsWarning)
        {
            GUILayout.BeginVertical(GUI.skin.box);

            GUILayout.Label(
                "Cosmetic controls here are a local browser UI. Equipment is only applied through supported game APIs.",
                smallStyle
            );

            if (GUILayout.Button("Dismiss"))
                cosmeticsWarning = false;

            GUILayout.EndVertical();
        }

        GUILayout.Space(6);

        GUILayout.BeginHorizontal();

        for (int i = 0;
             i < cosmeticCategories.Length;
             i++)
        {
            if (GUILayout.Button(
                cosmeticCategories[i],
                GUILayout.Height(30)
            ))
                cosmeticCategory = i;
        }

        GUILayout.EndHorizontal();

        GUILayout.Space(8);

        GUILayout.Label(
            cosmeticCategories[cosmeticCategory],
            titleStyle
        );

        GUILayout.BeginScrollView(
            Vector2.zero,
            GUILayout.Height(260)
        );

        for (int i = 0;
             i < cosmeticItems.Length;
             i++)
        {
            if (GUILayout.Button(
                cosmeticItems[i],
                GUILayout.Height(34)
            ))
            {
                statusText =
                    "Selected " + cosmeticItems[i];

                AddNotification(
                    "Selected: " + cosmeticItems[i]
                );
            }
        }

        GUILayout.EndScrollView();
    }

    // =========================================================
    // HOST
    // =========================================================

    private void DrawHostTab()
    {
        GUILayout.Label("HOST ONLY", titleStyle);
        GUILayout.Space(6);

        GUILayout.BeginHorizontal();

        for (int i = 0;
             i < hostSections.Length;
             i++)
        {
            if (GUILayout.Button(
                hostSections[i],
                GUILayout.Height(30)
            ))
                hostSection = i;
        }

        GUILayout.EndHorizontal();

        GUILayout.Space(8);

        switch (hostSection)
        {
            case 0:
                DrawHostOverview();
                break;

            case 1:
                DrawHostPlayers();
                break;

            case 2:
                DrawHostLobby();
                break;

            case 3:
                DrawHostPresets();
                break;
        }

        if (hostConfirmation)
            DrawHostConfirmation();
    }

    private void DrawHostOverview()
    {
        GUILayout.BeginVertical(GUI.skin.box);

        GUILayout.Label(
            "Host controls",
            titleStyle
        );

        GUILayout.Label(
            "These controls are UI hooks until the exact game networking API is connected.",
            smallStyle
        );

        GUILayout.EndVertical();

        if (GUILayout.Button(
            "Refresh Host State",
            GUILayout.Height(34)
        ))
        {
            AddNotification(
                "Host state refreshed."
            );
        }
    }

    private void DrawHostPlayers()
    {
        GUILayout.Label(
            "Player Management",
            labelStyle
        );

        for (int i = 0; i < players.Count; i++)
        {
            PlayerDisplayData player = players[i];

            GUILayout.BeginHorizontal(GUI.skin.box);

            GUILayout.Label(
                player.Name,
                smallStyle
            );

            if (GUILayout.Button(
                "Kick",
                GUILayout.Width(65)
            ))
            {
                AskHostAction("Kick " + player.Name);
            }

            if (GUILayout.Button(
                "Ban",
                GUILayout.Width(65)
            ))
            {
                AskHostAction("Ban " + player.Name);
            }

            GUILayout.EndHorizontal();
        }
    }

    private void DrawHostLobby()
    {
        GUILayout.Label("Lobby", titleStyle);

        if (GUILayout.Button(
            "Start Game",
            GUILayout.Height(36)
        ))
            AskHostAction("Start Game");

        if (GUILayout.Button(
            "Return To Lobby",
            GUILayout.Height(36)
        ))
            AskHostAction("Return To Lobby");

        if (GUILayout.Button(
            "Refresh Lobby",
            GUILayout.Height(36)
        ))
            AddNotification("Lobby refreshed.");
    }

    private void DrawHostPresets()
    {
        GUILayout.Label(
            "Host Presets",
            titleStyle
        );

        for (int i = 0;
             i < presetNames.Length;
             i++)
        {
            if (GUILayout.Button(
                "Load " + presetNames[i],
                GUILayout.Height(32)
            ))
                LoadPreset(i);
        }

        if (GUILayout.Button(
            "Save Current Preset",
            GUILayout.Height(32)
        ))
        {
            SaveSettings();
            AddNotification("Preset saved.");
        }
    }

    private void AskHostAction(string action)
    {
        pendingHostAction = action;
        hostConfirmation = true;
    }

    private void DrawHostConfirmation()
    {
        GUILayout.BeginVertical(GUI.skin.box);

        GUILayout.Label("Confirm", titleStyle);

        GUILayout.Label(
            pendingHostAction,
            labelStyle
        );

        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Cancel"))
            hostConfirmation = false;

        if (GUILayout.Button("Confirm"))
        {
            statusText =
                pendingHostAction +
                " requested.";

            AddNotification(
                pendingHostAction +
                " requested."
            );

            hostConfirmation = false;
        }

        GUILayout.EndHorizontal();

        GUILayout.EndVertical();
    }

    // =========================================================
    // MISC
    // =========================================================

    private void DrawMiscTab()
    {
        GUILayout.Label("MISC", titleStyle);
        GUILayout.Space(8);

        GUILayout.Label(
            statusText,
            labelStyle
        );

        notificationsEnabled =
            GUILayout.Toggle(
                notificationsEnabled,
                "Notifications"
            );

        debugInfo =
            GUILayout.Toggle(
                debugInfo,
                "Debug Information"
            );

        GUILayout.Space(8);

        if (GUILayout.Button("Save Settings"))
        {
            SaveSettings();
            AddNotification("Settings saved.");
        }

        if (GUILayout.Button("Reset Everything"))
        {
            ResetEverything();
            AddNotification("Settings reset.");
        }

        if (debugInfo)
        {
            GUILayout.Space(8);

            GUILayout.BeginVertical(GUI.skin.box);

            GUILayout.Label(
                "FPS: " + fps.ToString("0.0"),
                smallStyle
            );

            GUILayout.Label(
                "Screen: " +
                Screen.width +
                "x" +
                Screen.height,
                smallStyle
            );

            GUILayout.Label(
                "Tab: " + tabs[currentTab],
                smallStyle
            );

            GUILayout.Label(
                "Animation: " +
                menuAnimation.ToString("0.00"),
                smallStyle
            );

            GUILayout.EndVertical();
        }
    }

    // =========================================================
    // ABOUT
    // =========================================================

    private void DrawAboutTab()
    {
        GUILayout.Label("ABOUT", titleStyle);
        GUILayout.Space(15);

        GUILayout.Label("Jello", titleStyle);

        GUILayout.Label(
            "Version 1.1.0",
            labelStyle
        );

        GUILayout.Space(12);

        GUILayout.Label(
            "One-file QOL menu.",
            labelStyle
        );

        GUILayout.Label(
            "Status: " + statusText,
            smallStyle
        );

        GUILayout.Space(12);

        GUILayout.Label(
            "Features currently implemented:",
            labelStyle
        );

        GUILayout.Label("• Animated UI", smallStyle);
        GUILayout.Label("• HUD customization", smallStyle);
        GUILayout.Label("• FPS history", smallStyle);
        GUILayout.Label("• Player filtering", smallStyle);
        GUILayout.Label("• Idle tracking framework", smallStyle);
        GUILayout.Label("• Themes", smallStyle);
        GUILayout.Label("• Persistent settings", smallStyle);
        GUILayout.Label("• Host management UI", smallStyle);
        GUILayout.Label("• Host presets", smallStyle);
    }

    // =========================================================
    // HUD DRAW
    // =========================================================

    private void DrawHUD()
    {
        if (!hudEnabled)
            return;

        Color old = GUI.color;
        GUI.color = hudColor;

        if (showFPS)
        {
            GUI.Label(
                fpsRect,
                "FPS: " + fps.ToString("0"),
                hudStyle
            );
        }

        if (showClock)
        {
            GUI.Label(
                clockRect,
                DateTime.Now.ToString("HH:mm:ss"),
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
                "Runtime: " +
                runtime.ToString(@"hh\:mm\:ss"),
                hudStyle
            );
        }

        if (showResolution)
        {
            GUI.Label(
                resolutionRect,
                "Resolution: " +
                Screen.width +
                "x" +
                Screen.height,
                hudStyle
            );
        }

        if (showFPSGraph)
            DrawFPSGraph();

        if (showStatus)
        {
            GUI.Label(
                new Rect(15, 215, 350, 28),
                statusText,
                hudStyle
            );
        }

        GUI.color = old;
    }

    private void DrawFPSGraph()
    {
        GUI.Box(graphRect, "FPS");

        float max =
            Mathf.Max(120f, fpsWarning);

        for (int i = 0;
             i < fpsHistory.Length - 1;
             i++)
        {
            int a =
                (fpsHistoryIndex + i) %
                fpsHistory.Length;

            int b =
                (fpsHistoryIndex + i + 1) %
                fpsHistory.Length;

            float y1 =
                Mathf.Lerp(
                    graphRect.yMax - 8,
                    graphRect.y + 8,
                    Mathf.Clamp01(
                        fpsHistory[a] / max
                    )
                );

            float y2 =
                Mathf.Lerp(
                    graphRect.yMax - 8,
                    graphRect.y + 8,
                    Mathf.Clamp01(
                        fpsHistory[b] / max
                    )
                );

            DrawLine(
                new Vector2(
                    graphRect.x +
                    i * graphRect.width /
                    fpsHistory.Length,
                    y1
                ),
                new Vector2(
                    graphRect.x +
                    (i + 1) * graphRect.width /
                    fpsHistory.Length,
                    y2
                ),
                GetFPSColor(),
                2f
            );
        }
    }

    // =========================================================
    // PLAYER OVERLAY
    // =========================================================

    private void DrawPlayersOverlay()
    {
        Rect rect =
            new Rect(15, 270, 300, 260);

        GUI.Box(rect, "Players");

        GUILayout.BeginArea(
            new Rect(
                rect.x + 10,
                rect.y + 30,
                rect.width - 20,
                rect.height - 40
            )
        );

        for (int i = 0;
             i < players.Count;
             i++)
        {
            PlayerDisplayData p = players[i];

            GUILayout.Label(
                p.Name +
                " | " +
                p.Color +
                " | " +
                FormatTime(p.IdleTime),
                smallStyle
            );
        }

        GUILayout.EndArea();
    }

    // =========================================================
    // NOTIFICATIONS
    // =========================================================

    private void AddNotification(string text)
    {
        statusText = text;

        if (!notificationsEnabled)
            return;

        notifications.Add(text);

        while (notifications.Count > 4)
            notifications.RemoveAt(0);

        notificationTimer = 3f;
    }

    private void DrawNotifications()
    {
        if (!notificationsEnabled)
            return;

        if (notificationTimer <= 0f)
            return;

        float y = 20f;

        for (int i = notifications.Count - 1;
             i >= 0;
             i--)
        {
            GUI.Box(
                new Rect(
                    Screen.width - 330,
                    y,
                    310,
                    30
                ),
                notifications[i]
            );

            y += 34f;
        }
    }

    // =========================================================
    // PLAYERS
    // =========================================================

    private void BuildDemoPlayerList()
    {
        players.Clear();

        players.Add(
            new PlayerDisplayData(
                "Player",
                "Unknown",
                "Unknown",
                "Active"
            )
        );
    }

    private void RefreshPlayers()
    {
        if (players.Count == 0)
            BuildDemoPlayerList();

        foreach (PlayerDisplayData player in players)
            player.LastUpdate = Time.unscaledTime;
    }

    private void UpdatePlayers()
    {
        playerRefreshTimer +=
            Time.unscaledDeltaTime;

        if (playerRefreshTimer < 1f)
            return;

        playerRefreshTimer = 0f;

        foreach (PlayerDisplayData player in players)
        {
            if (Time.unscaledTime -
                player.LastUpdate > 90f)
            {
                player.Status = "Idle";
            }
            else
            {
                player.Status = "Active";
            }

            player.IdleTime =
                Time.unscaledTime -
                player.LastUpdate;
        }

        if (sortPlayers)
        {
            players.Sort(
                delegate (
                    PlayerDisplayData a,
                    PlayerDisplayData b)
                {
                    return string.Compare(
                        a.Name,
                        b.Name,
                        StringComparison.OrdinalIgnoreCase
                    );
                }
            );
        }
    }

    // =========================================================
    // THEMES
    // =========================================================

    private void ApplyTheme(int value)
    {
        theme = value;

        ApplyThemeSilent(value);

        SaveSettings();

        AddNotification("Theme changed.");
    }

    private void ApplyThemeSilent(int value)
    {
        switch (value)
        {
            case 1:
                backgroundColor =
                    new Color(0.03f, 0.08f, 0.10f);

                accentColor = Color.cyan;
                break;

            case 2:
                backgroundColor =
                    new Color(0.08f, 0.04f, 0.12f);

                accentColor =
                    new Color(0.7f, 0.3f, 1f);
                break;

            case 3:
                backgroundColor =
                    new Color(0.12f, 0.035f, 0.035f);

                accentColor = Color.red;
                break;

            case 4:
                backgroundColor =
                    new Color(0.025f, 0.025f, 0.025f);

                accentColor = Color.white;
                break;

            default:
                backgroundColor =
                    new Color(0.04f, 0.06f, 0.05f);

                accentColor =
                    new Color(0.25f, 1f, 0.5f);
                break;
        }
    }

    // =========================================================
    // PRESETS
    // =========================================================

    private void LoadPreset(int preset)
    {
        if (preset < 0 ||
            preset >= presetNames.Length)
            return;

        selectedPreset = preset;

        switch (preset)
        {
            case 0:
                animationsEnabled = true;
                animationSpeed = 8f;
                menuOpacity = 1f;
                uiScale = 1f;
                backgroundTransparency = 0f;
                compactMode = false;
                break;

            case 1:
                animationsEnabled = true;
                animationSpeed = 8f;
                menuOpacity = 0.95f;
                uiScale = 0.9f;
                backgroundTransparency = 0.05f;
                compactMode = true;
                break;

            case 2:
                animationsEnabled = false;
                animationSpeed = 8f;
                menuOpacity = 1f;
                uiScale = 0.9f;
                backgroundTransparency = 0f;
                compactMode = true;
                break;

            case 3:
                animationsEnabled = true;
                animationSpeed = 10f;
                menuOpacity = 0.95f;
                uiScale = 1f;
                backgroundTransparency = 0.1f;
                compactMode = false;
                break;
        }

        SaveSettings();

        AddNotification(
            "Loaded preset: " +
            presetNames[preset]
        );
    }

    // =========================================================
    // WINDOW
    // =========================================================

    private void DrawWindowBackground()
    {
        Color old = GUI.color;

        Color color = backgroundColor;

        color.a =
            1f - backgroundTransparency;

        GUI.color = color;

        GUI.Box(
            new Rect(
                0,
                0,
                window.width,
                window.height
            ),
            GUIContent.none
        );

        GUI.color = old;
    }

    // =========================================================
    // HUD MOVEMENT
    // =========================================================

    private void MoveHUD(float x, float y)
    {
        Vector2 movement = new Vector2(x, y);

        fpsRect.position += movement;
        clockRect.position += movement;
        runtimeRect.position += movement;
        resolutionRect.position += movement;
        graphRect.position += movement;
    }

    private void ResetHUD()
    {
        fpsRect = new Rect(15, 15, 180, 30);
        clockRect = new Rect(15, 45, 180, 30);
        runtimeRect = new Rect(15, 75, 220, 30);
        resolutionRect = new Rect(15, 105, 250, 30);
        graphRect = new Rect(15, 140, 220, 70);
    }

    private void ResetEverything()
    {
        menuOpacity = 1f;
        uiScale = 1f;
        backgroundTransparency = 0f;
        animationSpeed = 8f;
        animationsEnabled = true;
        compactMode = false;
        theme = 0;

        backgroundColor =
            new Color(0.04f, 0.045f, 0.06f, 1f);

        accentColor =
            new Color(0.25f, 1f, 0.5f, 1f);

        hudColor = Color.white;
        fpsWarning = 30f;

        ResetHUD();
        SaveSettings();
    }

    // =========================================================
    // SETTINGS
    // =========================================================

    private void SaveSettings()
    {
        PlayerPrefs.SetString(
            "Jello_MenuKey",
            menuKey.ToString()
        );

        PlayerPrefs.SetFloat(
            "Jello_Opacity",
            menuOpacity
        );

        PlayerPrefs.SetFloat(
            "Jello_Scale",
            uiScale
        );

        PlayerPrefs.SetFloat(
            "Jello_Transparency",
            backgroundTransparency
        );

        PlayerPrefs.SetFloat(
            "Jello_AnimationSpeed",
            animationSpeed
        );

        PlayerPrefs.SetInt(
            "Jello_Animations",
            animationsEnabled ? 1 : 0
        );

        PlayerPrefs.SetInt(
            "Jello_Theme",
            theme
        );

        PlayerPrefs.Save();
    }

    private void LoadSettings()
    {
        string savedKey =
            PlayerPrefs.GetString(
                "Jello_MenuKey",
                "Delete"
            );

        KeyCode loaded;

        if (Enum.TryParse(
            savedKey,
            out loaded))
        {
            menuKey = loaded;
        }

        menuOpacity =
            PlayerPrefs.GetFloat(
                "Jello_Opacity",
                1f
            );

        uiScale =
            PlayerPrefs.GetFloat(
                "Jello_Scale",
                1f
            );

        backgroundTransparency =
            PlayerPrefs.GetFloat(
                "Jello_Transparency",
                0f
            );

        animationSpeed =
            PlayerPrefs.GetFloat(
                "Jello_AnimationSpeed",
                8f
            );

        animationsEnabled =
            PlayerPrefs.GetInt(
                "Jello_Animations",
                1
            ) == 1;

        theme =
            PlayerPrefs.GetInt(
                "Jello_Theme",
                0
            );

        ApplyThemeSilent(theme);
    }

    // =========================================================
    // UTILITY
    // =========================================================

    private string FormatTime(float seconds)
    {
        TimeSpan time =
            TimeSpan.FromSeconds(
                Mathf.Max(0f, seconds)
            );

        return time.ToString(@"mm\:ss");
    }

    private Color GetFPSColor()
    {
        if (fps >= fpsWarning)
            return Color.green;

        if (fps >= fpsWarning * 0.66f)
            return Color.yellow;

        return Color.red;
    }

    private float EaseOutCubic(float value)
    {
        value = Mathf.Clamp01(value);

        return 1f -
            Mathf.Pow(
                1f - value,
                3f
            );
    }

    // =========================================================
    // LINE DRAWING
    // =========================================================

    private void DrawLine(
        Vector2 start,
        Vector2 end,
        Color color,
        float width)
    {
        Color old = GUI.color;

        GUI.color = color;

        Matrix4x4 matrix = GUI.matrix;

        float angle =
            Vector3.Angle(
                end - start,
                Vector2.right
            );

        if (start.y > end.y)
            angle = -angle;

        float length =
            Vector2.Distance(start, end);

        GUIUtility.RotateAroundPivot(
            angle,
            start
        );

        GUI.DrawTexture(
            new Rect(
                start.x,
                start.y,
                length,
                width
            ),
            Texture2D.whiteTexture
        );

        GUI.matrix = matrix;
        GUI.color = old;
    }

    // =========================================================
    // STYLES
    // =========================================================

    private void CreateStyles()
    {
        if (titleStyle != null)
            return;

        titleStyle =
            new GUIStyle(GUI.skin.label);

        titleStyle.fontSize = 20;
        titleStyle.fontStyle =
            FontStyle.Bold;

        titleStyle.alignment =
            TextAnchor.MiddleCenter;

        labelStyle =
            new GUIStyle(GUI.skin.label);

        labelStyle.fontSize = 14;
        labelStyle.alignment =
            TextAnchor.MiddleCenter;

        hudStyle =
            new GUIStyle(GUI.skin.label);

        hudStyle.fontSize = 16;
        hudStyle.fontStyle =
            FontStyle.Bold;

        smallStyle =
            new GUIStyle(GUI.skin.label);

        smallStyle.fontSize = 12;
        smallStyle.alignment =
            TextAnchor.MiddleCenter;

        boxStyle =
            new GUIStyle(GUI.skin.box);
    }
}

// =========================================================
// PLAYER DATA
// =========================================================

public class PlayerDisplayData
{
    public string Name;
    public string Color;
    public string Device;
    public string Status;

    public float IdleTime;
    public float LastUpdate;

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

        IdleTime = 0f;
        LastUpdate = Time.unscaledTime;
    }
}
