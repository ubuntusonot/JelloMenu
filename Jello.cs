using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using UnityEngine;

[BepInPlugin("org.vinegar.gfr", "Jello", "2.0.0")]
public sealed class QOLMenuPlugin : BasePlugin
{
    public override void Load()
    {
        Log.LogInfo("[Jello] Loading Jello 2.0.0");

        try
        {
            AddComponent<JelloMenu>();
            Log.LogInfo("[Jello] Jello 2.0.0 loaded successfully.");
        }
        catch (Exception e)
        {
            Log.LogError("[Jello] Failed to load: " + e);
        }
    }
}

public sealed class JelloMenu : MonoBehaviour
{
    // =========================================================
    // CONSTANTS
    // =========================================================

    private const string Prefix = "Jello_";
    private const string Version = "2.0.0";

    private const float AnimationSpeed = 10f;
    private const float NotificationDuration = 3f;

    // =========================================================
    // MENU STATE
    // =========================================================

    private bool open;
    private bool maximized;
    private bool dragging;
    private bool resizing;
    private bool listeningForKey;

    private bool animations = true;
    private bool notifications = true;
    private bool compact;
    private bool debug;
    private bool performanceMode;

    private KeyCode menuKey = KeyCode.Delete;

    private Rect window =
        new Rect(250, 120, 780, 590);

    private Rect normalWindow;

    private Vector2 scroll;
    private Vector2 dragOffset;
    private Vector2 resizeStartMouse;
    private Vector2 resizeStartSize;

    private int tab;
    private int cosmeticTab;
    private int hostTab;

    private float menuAnim;
    private float opacity = 1f;
    private float scale = 1f;

    // =========================================================
    // TABS
    // =========================================================

    private readonly string[] tabs =
    {
        "HUD",
        "Players",
        "UI",
        "Keybinds",
        "Cosmetics",
        "Host",
        "Misc",
        "About"
    };

    // =========================================================
    // SEARCH / FAVORITES
    // =========================================================

    private string search = "";
    private bool favoritesOnly;

    private readonly HashSet<string> favorites =
        new HashSet<string>();

    // =========================================================
    // HUD
    // =========================================================

    private bool hud = true;
    private bool fpsEnabled = true;
    private bool clockEnabled = true;
    private bool runtimeEnabled;
    private bool resolutionEnabled;
    private bool graphEnabled;
    private bool statusEnabled = true;

    private bool frameTimeEnabled;
    private bool fpsStatsEnabled;
    private bool refreshRateEnabled;
    private bool qualityEnabled;

    private float fps;
    private float minFPS = float.MaxValue;
    private float maxFPS;
    private float averageFPS;
    private float fpsTotal;
    private int fpsSamples;

    private float frameTime;

    private float fpsTime;
    private int fpsFrames;
    private int fpsIndex;

    private readonly float[] fpsHistory =
        new float[60];

    private Rect fpsRect =
        new Rect(15, 15, 220, 28);

    private Rect clockRect =
        new Rect(15, 45, 220, 28);

    private Rect runtimeRect =
        new Rect(15, 75, 260, 28);

    private Rect resolutionRect =
        new Rect(15, 105, 260, 28);

    private Rect frameTimeRect =
        new Rect(15, 135, 260, 28);

    private string clockText = "";
    private string runtimeText = "";
    private string resolutionText = "";
    private string refreshRateText = "";
    private string qualityText = "";

    private float hudTimer;

    // =========================================================
    // PLAYER SYSTEM
    // =========================================================

    private bool playerOverlay;
    private bool playerColor = true;
    private bool playerStatus = true;
    private bool playerDevice = true;
    private bool idleTime = true;
    private bool alphabetical;

    private readonly List<PlayerData> players =
        new List<PlayerData>();

    // =========================================================
    // COSMETICS
    // =========================================================

    private readonly string[] cosmeticTabs =
    {
        "Hats",
        "Skins",
        "Pets",
        "Visors",
        "Names"
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

    // =========================================================
    // HOST
    // =========================================================

    private readonly string[] hostTabs =
    {
        "Overview",
        "Players",
        "Lobby",
        "Presets"
    };

    private string confirmation = "";

    // =========================================================
    // NOTIFICATIONS
    // =========================================================

    private string status = "";
    private float notificationUntil;

    private readonly List<string> notificationHistory =
        new List<string>();

    // =========================================================
    // THEMES
    // =========================================================

    private int theme;

    private readonly string[] themes =
    {
        "Dark",
        "Jello",
        "Blue",
        "Purple",
        "Red",
        "Green"
    };

    private Color accent =
        new Color(
            0.35f,
            0.65f,
            1f);

    // =========================================================
    // PROFILES
    // =========================================================

    private readonly string[] profiles =
    {
        "Default",
        "Minimal",
        "Performance",
        "Streamer"
    };

    // =========================================================
    // STYLES
    // =========================================================

    private GUIStyle title;
    private GUIStyle windowTitle;
    private GUIStyle label;
    private GUIStyle small;
    private GUIStyle hudStyle;

    private GUIStyle buttonStyle;
    private GUIStyle tabStyle;
    private GUIStyle closeStyle;
    private GUIStyle maxStyle;
    private GUIStyle toggleStyle;
    private GUIStyle panelStyle;

    // =========================================================
    // UNITY START
    // =========================================================

    private void Start()
    {
        LoadSettings();

        if (players.Count == 0)
        {
            players.Add(
                new PlayerData(
                    "Player",
                    "Unknown",
                    "Unknown"));
        }

        UpdateHUDCache();

        Debug.Log(
            "[Jello] Started. Menu key: " +
            menuKey);
    }

    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        UpdateFPS();
        UpdateHUDCache();
        UpdatePlayerIdle();

        HandleMenuKey();
        HandleEscape();

        UpdateAnimation();
        UpdateNotification();
    }

    private void HandleMenuKey()
    {
        if (Input.GetKeyDown(menuKey))
        {
            open = !open;

            Notify(
                open
                    ? "Jello opened."
                    : "Jello closed.");
        }
    }

    private void HandleEscape()
    {
        if (!open)
            return;

        if (Input.GetKeyDown(KeyCode.Escape) &&
            !listeningForKey)
        {
            open = false;
        }
    }

    private void UpdateAnimation()
    {
        float target =
            open ? 1f : 0f;

        menuAnim =
            animations
                ? Mathf.MoveTowards(
                    menuAnim,
                    target,
                    Time.unscaledDeltaTime *
                    AnimationSpeed)
                : target;
    }

    // =========================================================
    // GUI
    // =========================================================

    private void OnGUI()
    {
        EnsureStyles();

        DrawHUD();

        if (playerOverlay)
            DrawPlayerOverlay();

        DrawNotifications();

        if (menuAnim > 0.001f)
            DrawMenu();
    }

    // =========================================================
    // STYLES
    // =========================================================

    private void EnsureStyles()
    {
        if (title == null)
            BuildStyles();
    }

    private void BuildStyles()
    {
        title = new GUIStyle(GUI.skin.label)
        {
            fontSize = 20,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft
        };

        windowTitle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 17,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft
        };

        label = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            alignment = TextAnchor.MiddleLeft
        };

        small = new GUIStyle(GUI.skin.label)
        {
            fontSize = 12,
            alignment = TextAnchor.MiddleLeft
        };

        hudStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 16,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft
        };

        buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 13,
            alignment = TextAnchor.MiddleCenter
        };

        tabStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 13,
            alignment = TextAnchor.MiddleLeft
        };

        closeStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 18,
            alignment = TextAnchor.MiddleCenter
        };

        maxStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 16,
            alignment = TextAnchor.MiddleCenter
        };

        toggleStyle = new GUIStyle(GUI.skin.toggle)
        {
            fontSize = 13
        };

        panelStyle = new GUIStyle(GUI.skin.box);
    }

    // =========================================================
    // MENU
    // =========================================================

    private void DrawMenu()
    {
        Color oldColor = GUI.color;

        GUI.color = new Color(
            1f,
            1f,
            1f,
            Ease(menuAnim) * opacity);

        if (maximized)
        {
            window = new Rect(
                25,
                25,
                Screen.width - 50,
                Screen.height - 50);
        }

        DrawBackdrop();
        DrawWindow();

        GUI.color = oldColor;
    }

    private void DrawBackdrop()
    {
        Color old = GUI.color;

        GUI.color = new Color(
            0f,
            0f,
            0f,
            .30f * Ease(menuAnim));

        GUI.DrawTexture(
            new Rect(
                0,
                0,
                Screen.width,
                Screen.height),
            Texture2D.whiteTexture);

        GUI.color = old;
    }

    private void DrawWindow()
    {
        Rect r = window;

        GUI.Box(
            r,
            GUIContent.none);

        DrawTitleBar(r);
        DrawSidebar(r);
        DrawContent(r);

        if (!maximized)
            DrawResizeHandle(r);
    }

    // =========================================================
    // TITLE BAR
    // =========================================================

    private void DrawTitleBar(Rect r)
    {
        Rect bar =
            new Rect(
                r.x,
                r.y,
                r.width,
                48);

        GUI.Label(
            new Rect(
                r.x + 10,
                r.y + 5,
                100,
                38),
            "Jello",
            windowTitle);

        GUI.Label(
            new Rect(
                r.x + 110,
                r.y + 8,
                100,
                30),
            Version,
            small);

        if (GUI.Button(
            new Rect(
                r.x + r.width - 112,
                r.y + 8,
                30,
                30),
            "−",
            maxStyle))
        {
            open = false;
        }

        if (GUI.Button(
            new Rect(
                r.x + r.width - 78,
                r.y + 8,
                30,
                30),
            maximized ? "❐" : "□",
            maxStyle))
        {
            ToggleMaximize();
        }

        if (GUI.Button(
            new Rect(
                r.x + r.width - 42,
                r.y + 8,
                30,
                30),
            "×",
            closeStyle))
        {
            open = false;
        }

        HandleDrag(bar);
    }

    // =========================================================
    // DRAGGING
    // =========================================================

    private void HandleDrag(Rect bar)
    {
        if (maximized || resizing)
            return;

        Event e = Event.current;

        if (e == null)
            return;

        if (e.type == EventType.MouseDown &&
            e.button == 0 &&
            bar.Contains(e.mousePosition))
        {
            dragging = true;

            dragOffset =
                e.mousePosition -
                new Vector2(
                    window.x,
                    window.y);

            e.Use();
        }

        if (e.type == EventType.MouseDrag &&
            e.button == 0 &&
            dragging)
        {
            window.position =
                e.mousePosition -
                dragOffset;

            ClampWindow();

            e.Use();
        }

        if (e.type == EventType.MouseUp &&
            e.button == 0)
        {
            dragging = false;

            SaveWindow();
        }
    }

    // =========================================================
    // RESIZING
    // =========================================================

    private void DrawResizeHandle(Rect r)
    {
        Rect handle =
            new Rect(
                r.xMax - 18,
                r.yMax - 18,
                18,
                18);

        GUI.Label(
            handle,
            "◢",
            small);

        Event e = Event.current;

        if (e == null)
            return;

        if (e.type == EventType.MouseDown &&
            e.button == 0 &&
            handle.Contains(e.mousePosition))
        {
            resizing = true;
            resizeStartMouse = e.mousePosition;
            resizeStartSize = window.size;

            e.Use();
        }

        if (e.type == EventType.MouseDrag &&
            e.button == 0 &&
            resizing)
        {
            Vector2 delta =
                e.mousePosition -
                resizeStartMouse;

            window.width =
                Mathf.Max(
                    560,
                    resizeStartSize.x +
                    delta.x);

            window.height =
                Mathf.Max(
                    420,
                    resizeStartSize.y +
                    delta.y);

            ClampWindow();

            e.Use();
        }

        if (e.type == EventType.MouseUp &&
            e.button == 0)
        {
            resizing = false;

            SaveWindow();

            e.Use();
        }
    }

    private void ClampWindow()
    {
        window.x =
            Mathf.Clamp(
                window.x,
                0,
                Mathf.Max(
                    0,
                    Screen.width -
                    window.width));

        window.y =
            Mathf.Clamp(
                window.y,
                0,
                Mathf.Max(
                    0,
                    Screen.height -
                    window.height));
    }

    private void ToggleMaximize()
    {
        if (!maximized)
        {
            normalWindow = window;
            maximized = true;

            Notify("Window maximized.");
        }
        else
        {
            maximized = false;
            window = normalWindow;

            Notify("Window restored.");
        }
    }

    // =========================================================
    // SIDEBAR
    // =========================================================

    private void DrawSidebar(Rect r)
    {
        float width =
            compact ? 135 : 175;

        Rect side =
            new Rect(
                r.x,
                r.y + 48,
                width,
                r.height - 48);

        GUI.Box(
            side,
            GUIContent.none);

        GUI.Label(
            new Rect(
                side.x + 10,
                side.y + 10,
                side.width - 20,
                30),
            "JELLO",
            title);

        float y =
            side.y + 50;

        for (int i = 0;
             i < tabs.Length;
             i++)
        {
            Rect tr =
                new Rect(
                    side.x + 10,
                    y,
                    side.width - 20,
                    32);

            if (GUI.Button(
                tr,
                tabs[i],
                tabStyle))
            {
                tab = i;
                scroll = Vector2.zero;
            }

            y += 36;
        }

        GUI.Label(
            new Rect(
                side.x + 10,
                side.y + side.height - 75,
                side.width - 20,
                20),
            "Jello " + Version,
            small);

        if (GUI.Button(
            new Rect(
                side.x + 10,
                side.y + side.height - 42,
                side.width - 20,
                32),
            "Close",
            buttonStyle))
        {
            open = false;
        }
    }

    // =========================================================
    // CONTENT
    // =========================================================

    private void DrawContent(Rect r)
    {
        float sidebarWidth =
            compact ? 135 : 175;

        Rect content =
            new Rect(
                r.x + sidebarWidth,
                r.y + 48,
                r.width - sidebarWidth,
                r.height - 48);

        GUI.Box(
            content,
            GUIContent.none);

        GUILayout.BeginArea(
            new Rect(
                content.x + 15,
                content.y + 15,
                content.width - 30,
                content.height - 30));

        DrawSearch();

        scroll =
            GUILayout.BeginScrollView(
                scroll);

        switch (tab)
        {
            case 0:
                HUDTab();
                break;

            case 1:
                PlayersTab();
                break;

            case 2:
                UITab();
                break;

            case 3:
                KeybindTab();
                break;

            case 4:
                CosmeticsTab();
                break;

            case 5:
                HostTab();
                break;

            case 6:
                MiscTab();
                break;

            case 7:
                AboutTab();
                break;
        }

        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    // =========================================================
    // SEARCH
    // =========================================================

    private void DrawSearch()
    {
        GUILayout.BeginHorizontal();

        GUILayout.Label(
            "Search:",
            small,
            GUILayout.Width(50));

        search =
            GUILayout.TextField(
                search ?? "",
                GUILayout.Height(25));

        if (GUILayout.Button(
            "Clear",
            buttonStyle,
            GUILayout.Width(60)))
        {
            search = "";
        }

        GUILayout.EndHorizontal();

        GUILayout.Space(8);
    }

    // =========================================================
    // HUD TAB
    // =========================================================

    private void HUDTab()
    {
        Header("HUD");

        hud =
            Toggle(
                hud,
                "Enable HUD",
                "hud");

        fpsEnabled =
            Toggle(
                fpsEnabled,
                "FPS Counter",
                "fps");

        frameTimeEnabled =
            Toggle(
                frameTimeEnabled,
                "Frame Time",
                "frametime");

        clockEnabled =
            Toggle(
                clockEnabled,
                "Clock",
                "clock");

        runtimeEnabled =
            Toggle(
                runtimeEnabled,
                "Runtime",
                "runtime");

        resolutionEnabled =
            Toggle(
                resolutionEnabled,
                "Resolution",
                "resolution");

        refreshRateEnabled =
            Toggle(
                refreshRateEnabled,
                "Refresh Rate",
                "refresh");

        qualityEnabled =
            Toggle(
                qualityEnabled,
                "Quality Level",
                "quality");

        graphEnabled =
            Toggle(
                graphEnabled,
                "FPS Graph",
                "graph");

        fpsStatsEnabled =
            Toggle(
                fpsStatsEnabled,
                "FPS Statistics",
                "stats");

        statusEnabled =
            Toggle(
                statusEnabled,
                "Status",
                "status");

        Space();

        GUILayout.Label(
            "FPS Warning: " +
            GetFPSWarningText(),
            label);

        Space();

        Button(
            "Move HUD Left",
            () => MoveHUD(-10, 0));

        Button(
            "Move HUD Right",
            () => MoveHUD(10, 0));

        Button(
            "Move HUD Up",
            () => MoveHUD(0, -10));

        Button(
            "Move HUD Down",
            () => MoveHUD(0, 10));

        Button(
            "Reset HUD Position",
            ResetHUD);

        Space();

        GUILayout.Label(
            "FPS Limit Warning",
            label);

        fpsLimit =
            GUILayout.HorizontalSlider(
                fpsLimit,
                10,
                240);

        GUILayout.Label(
            fpsLimit.ToString("0") +
            " FPS",
            small);
    }

    private float fpsLimit = 30f;

    private string GetFPSWarningText()
    {
        if (fps <= 0)
            return "Waiting...";

        return fps < fpsLimit
            ? "LOW FPS"
            : "Normal";
    }

    // =========================================================
    // PLAYERS TAB
    // =========================================================

    private void PlayersTab()
    {
        Header("PLAYERS");

        playerOverlay =
            Toggle(
                playerOverlay,
                "Player Overlay",
                "overlay");

        playerColor =
            Toggle(
                playerColor,
                "Color",
                "color");

        playerStatus =
            Toggle(
                playerStatus,
                "Status",
                "playerstatus");

        playerDevice =
            Toggle(
                playerDevice,
                "Device",
                "device");

        idleTime =
            Toggle(
                idleTime,
                "Idle Time",
                "idle");

        alphabetical =
            Toggle(
                alphabetical,
                "Sort Alphabetically",
                "alphabetical");

        Space();

        Button(
            "Refresh Players",
            RefreshPlayers);

        Button(
            "Add Test Player",
            AddTestPlayer);

        Button(
            "Clear Test Players",
            ClearTestPlayers);

        Space();

        DrawPlayers();
    }

    private void DrawPlayers()
    {
        string query =
            search?.Trim() ?? "";

        IEnumerable<PlayerData> result =
            players;

        if (!string.IsNullOrEmpty(query))
        {
            result =
                result.Where(
                    p =>
                        p.Name.IndexOf(
                            query,
                            StringComparison
                                .OrdinalIgnoreCase) >= 0);
        }

        if (alphabetical)
        {
            result =
                result.OrderBy(
                    p => p.Name,
                    StringComparer
                        .OrdinalIgnoreCase);
        }

        foreach (PlayerData player in result)
        {
            GUILayout.BeginVertical(
                panelStyle);

            GUILayout.Label(
                player.Name,
                label);

            if (playerColor)
            {
                GUILayout.Label(
                    "Color: " +
                    player.Color,
                    small);
            }

            if (playerStatus)
            {
                GUILayout.Label(
                    "Status: " +
                    player.Status,
                    small);
            }

            if (playerDevice)
            {
                GUILayout.Label(
                    "Device: " +
                    player.Device,
                    small);
            }

            if (idleTime)
            {
                GUILayout.Label(
                    "Idle: " +
                    FormatTime(player.Idle),
                    small);
            }

            GUILayout.EndVertical();

            GUILayout.Space(5);
        }

        GUILayout.Label(
            "Players: " +
            players.Count,
            small);
    }

    private void AddTestPlayer()
    {
        players.Add(
            new PlayerData(
                "Player " +
                (players.Count + 1),
                "Unknown",
                "Unknown"));

        Notify("Test player added.");
    }

    private void ClearTestPlayers()
    {
        while (players.Count > 1)
            players.RemoveAt(players.Count - 1);

        Notify("Test players cleared.");
    }

    // =========================================================
    // UI TAB
    // =========================================================

    private void UITab()
    {
        Header("UI");

        animations =
            Toggle(
                animations,
                "Animations",
                "animations");

        notifications =
            Toggle(
                notifications,
                "Notifications",
                "notifications");

        compact =
            Toggle(
                compact,
                "Compact Menu",
                "compact");

        performanceMode =
            Toggle(
                performanceMode,
                "Performance Mode",
                "performance");

        favoritesOnly =
            Toggle(
                favoritesOnly,
                "Favorites Only",
                "favorites");

        Space();

        GUILayout.Label(
            "Opacity: " +
            opacity.ToString("0.00"),
            label);

        opacity =
            GUILayout.HorizontalSlider(
                opacity,
                .25f,
                1f);

        GUILayout.Label(
            "Scale: " +
            scale.ToString("0.00"),
            label);

        scale =
            GUILayout.HorizontalSlider(
                scale,
                .75f,
                1.5f);

        Space();

        GUILayout.Label(
            "Theme",
            label);

        for (int i = 0;
             i < themes.Length;
             i++)
        {
            int id = i;

            if (GUILayout.Button(
                (theme == id ? "✓ " : "") +
                themes[id],
                buttonStyle))
            {
                theme = id;
                ApplyTheme(id);
                SaveSettings();

                Notify(
                    "Theme: " +
                    themes[id]);
            }
        }

        Space();

        Button(
            "Reset UI",
            ResetUI);
    }

    // =========================================================
    // KEYBINDS
    // =========================================================

    private void KeybindTab()
    {
        Header("KEYBINDS");

        GUILayout.Label(
            "Current Menu Key: " +
            menuKey,
            label);

        Space();

        if (listeningForKey)
        {
            GUILayout.Label(
                "Press any key...",
                title);

            GUILayout.Label(
                "Escape cancels.",
                small);

            ListenForKey();
        }
        else
        {
            Button(
                "Change Menu Key",
                () =>
                {
                    listeningForKey = true;

                    Notify(
                        "Press a key...");
                });
        }

        Button(
            "Reset Menu Key",
            () =>
            {
                menuKey =
                    KeyCode.Delete;

                listeningForKey = false;

                SaveSettings();

                Notify(
                    "Menu key reset.");
            });

        Space();

        GUILayout.Label(
            "Keybind Information",
            label);

        GUILayout.Label(
            "Menu: " + menuKey,
            small);

        GUILayout.Label(
            "Escape: Close menu",
            small);
    }

    private void ListenForKey()
    {
        Event e = Event.current;

        if (e == null ||
            e.type != EventType.KeyDown ||
            e.keyCode == KeyCode.None)
        {
            return;
        }

        if (e.keyCode == KeyCode.Escape)
        {
            listeningForKey = false;

            Notify(
                "Key change cancelled.");

            e.Use();
            return;
        }

        menuKey = e.keyCode;
        listeningForKey = false;

        SaveSettings();

        Notify(
            "Menu key changed to " +
            menuKey);

        e.Use();
    }

    // =========================================================
    // COSMETICS
    // =========================================================

    private void CosmeticsTab()
    {
        Header("COSMETICS");

        GUILayout.Label(
            "Cosmetic integration requires the game's actual supported API.",
            small);

        GUILayout.Label(
            "This menu provides the UI/integration layer without inventing game calls.",
            small);

        Space();

        GUILayout.BeginHorizontal();

        for (int i = 0;
             i < cosmeticTabs.Length;
             i++)
        {
            int id = i;

            if (GUILayout.Button(
                cosmeticTabs[i],
                buttonStyle))
            {
                cosmeticTab = id;
            }
        }

        GUILayout.EndHorizontal();

        Space();

        GUILayout.Label(
            cosmeticTabs[cosmeticTab],
            title);

        foreach (string item in cosmeticItems)
        {
            string selected = item;

            Button(
                IsFavorite(selected)
                    ? "★ " + selected
                    : "☆ " + selected,
                () =>
                {
                    ToggleFavorite(selected);

                    Notify(
                        "Selected: " +
                        selected);
                });
        }
    }

    // =========================================================
    // HOST TAB
    // =========================================================

    private void HostTab()
    {
        Header("HOST");

        GUILayout.BeginHorizontal();

        for (int i = 0;
             i < hostTabs.Length;
             i++)
        {
            int id = i;

            if (GUILayout.Button(
                hostTabs[i],
                buttonStyle))
            {
                hostTab = id;
            }
        }

        GUILayout.EndHorizontal();

        Space();

        switch (hostTab)
        {
            case 0:
                HostOverview();
                break;

            case 1:
                HostPlayers();
                break;

            case 2:
                HostLobby();
                break;

            case 3:
                HostPresets();
                break;
        }

        if (!string.IsNullOrEmpty(
            confirmation))
        {
            ConfirmHostAction();
        }
    }

    private void HostOverview()
    {
        GUILayout.BeginVertical(
            panelStyle);

        GUILayout.Label(
            "Host Controls",
            title);

        GUILayout.Label(
            "Players: " +
            players.Count,
            small);

        GUILayout.Label(
            "Game API: Integration required",
            small);

        GUILayout.EndVertical();

        Space();

        Button(
            "Refresh Lobby",
            () =>
                Notify(
                    "Lobby refresh requested."));
    }

    private void HostPlayers()
    {
        foreach (PlayerData player in players)
        {
            GUILayout.BeginHorizontal(
                panelStyle);

            GUILayout.Label(
                player.Name,
                small);

            if (GUILayout.Button(
                "Kick",
                buttonStyle,
                GUILayout.Width(65)))
            {
                Ask(
                    "Kick " +
                    player.Name);
            }

            if (GUILayout.Button(
                "Ban",
                buttonStyle,
                GUILayout.Width(65)))
            {
                Ask(
                    "Ban " +
                    player.Name);
            }

            GUILayout.EndHorizontal();

            GUILayout.Space(4);
        }
    }

    private void HostLobby()
    {
        Button(
            "Start Game",
            () => Ask("Start Game"));

        Button(
            "Return To Lobby",
            () => Ask("Return To Lobby"));

        Button(
            "Refresh Lobby",
            () => Notify(
                "Lobby refresh requested."));
    }

    private void HostPresets()
    {
        foreach (string profile in profiles)
        {
            string p = profile;

            Button(
                "Load " + p,
                () => LoadProfile(p));
        }

        Button(
            "Save Current Profile",
            SaveSettings);
    }

    private void Ask(string action)
    {
        confirmation = action;
    }

    private void ConfirmHostAction()
    {
        GUILayout.BeginVertical(
            panelStyle);

        GUILayout.Label(
            "Confirm Action",
            title);

        GUILayout.Label(
            confirmation,
            label);

        GUILayout.BeginHorizontal();

        if (GUILayout.Button(
            "Cancel",
            buttonStyle))
        {
            confirmation = "";
        }

        if (GUILayout.Button(
            "Confirm",
            buttonStyle))
        {
            Notify(
                confirmation +
                " requested.");

            confirmation = "";
        }

        GUILayout.EndHorizontal();

        GUILayout.EndVertical();
    }

    // =========================================================
    // MISC TAB
    // =========================================================

    private void MiscTab()
    {
        Header("MISC");

        notifications =
            Toggle(
                notifications,
                "Notifications",
                "notifications");

        debug =
            Toggle(
                debug,
                "Debug Information",
                "debug");

        performanceMode =
            Toggle(
                performanceMode,
                "Performance Mode",
                "performance");

        Space();

        Button(
            "Save Settings",
            SaveSettings);

        Button(
            "Load Settings",
            LoadSettings);

        Button(
            "Reset Everything",
            ResetEverything);

        Button(
            "Reset Notifications",
            () =>
            {
                notificationHistory.Clear();

                Notify(
                    "Notification history cleared.");
            });

        Space();

        DrawProfiles();

        if (!debug)
            return;

        Space();

        DrawDiagnostics();
    }

    private void DrawProfiles()
    {
        GUILayout.Label(
            "Profiles",
            title);

        foreach (string profile in profiles)
        {
            string p = profile;

            Button(
                "Load " + p,
                () => LoadProfile(p));
        }
    }

    private void DrawDiagnostics()
    {
        GUILayout.BeginVertical(
            panelStyle);

        GUILayout.Label(
            "Diagnostics",
            title);

        GUILayout.Label(
            "Jello: " + Version,
            small);

        GUILayout.Label(
            "FPS: " +
            fps.ToString("0.0"),
            small);

        GUILayout.Label(
            "Frame Time: " +
            frameTime.ToString("0.00") +
            " ms",
            small);

        GUILayout.Label(
            "Min FPS: " +
            GetMinFPS(),
            small);

        GUILayout.Label(
            "Max FPS: " +
            maxFPS.ToString("0"),
            small);

        GUILayout.Label(
            "Average FPS: " +
            averageFPS.ToString("0.0"),
            small);

        GUILayout.Label(
            "Resolution: " +
            resolutionText,
            small);

        GUILayout.Label(
            "Refresh Rate: " +
            refreshRateText,
            small);

        GUILayout.Label(
            "Unity: " +
            Application.unityVersion,
            small);

        GUILayout.Label(
            "Scene: " +
            GetSceneName(),
            small);

        GUILayout.Label(
            "Menu Key: " +
            menuKey,
            small);

        GUILayout.EndVertical();
    }

    // =========================================================
    // ABOUT TAB
    // =========================================================

    private void AboutTab()
    {
        Header("ABOUT");

        GUILayout.Label(
            "Jello",
            title);

        GUILayout.Label(
            "Version " +
            Version,
            label);

        Space();

        GUILayout.Label(
            "Single-file QOL menu framework.",
            label);

        GUILayout.Label(
            "Built for BepInEx IL2CPP.",
            small);

        Space();

        string[] features =
        {
            "Desktop-style window",
            "Window dragging",
            "Window resizing",
            "Maximize / restore",
            "Persistent settings",
            "Configurable menu key",
            "HUD customization",
            "FPS history",
            "FPS statistics",
            "Player filtering",
            "Player overlay",
            "Themes",
            "Profiles",
            "Notifications",
            "Diagnostics",
            "Performance mode"
        };

        foreach (string feature in features)
        {
            GUILayout.Label(
                "• " + feature,
                small);
        }

        Space();

        GUILayout.Label(
            "Game-specific integrations are intentionally isolated until the game's actual API is known.",
            small);
    }

    // =========================================================
    // HUD DRAW
    // =========================================================

    private void DrawHUD()
    {
        if (!hud)
            return;

        if (fpsEnabled)
        {
            GUI.Label(
                fpsRect,
                "FPS: " +
                fps.ToString("0"),
                hudStyle);
        }

        if (frameTimeEnabled)
        {
            GUI.Label(
                frameTimeRect,
                "Frame: " +
                frameTime.ToString("0.00") +
                " ms",
                hudStyle);
        }

        if (clockEnabled)
        {
            GUI.Label(
                clockRect,
                clockText,
                hudStyle);
        }

        if (runtimeEnabled)
        {
            GUI.Label(
                runtimeRect,
                runtimeText,
                hudStyle);
        }

        if (resolutionEnabled)
        {
            GUI.Label(
                resolutionRect,
                resolutionText,
                hudStyle);
        }

        if (refreshRateEnabled)
        {
            GUI.Label(
                new Rect(
                    15,
                    165,
                    300,
                    28),
                refreshRateText,
                hudStyle);
        }

        if (qualityEnabled)
        {
            GUI.Label(
                new Rect(
                    15,
                    195,
                    300,
                    28),
                qualityText,
                hudStyle);
        }

        if (statusEnabled &&
            !string.IsNullOrEmpty(status))
        {
            GUI.Label(
                new Rect(
                    15,
                    225,
                    400,
                    28),
                status,
                hudStyle);
        }

        if (fpsStatsEnabled)
            DrawFPSStats();

        if (graphEnabled)
            DrawFPSGraph();
    }

    private void DrawFPSStats()
    {
        Rect r =
            new Rect(
                15,
                255,
                300,
                85);

        GUI.Box(
            r,
            GUIContent.none);

        GUI.Label(
            new Rect(
                r.x + 10,
                r.y + 5,
                r.width - 20,
                20),
            "FPS Statistics",
            small);

        GUI.Label(
            new Rect(
                r.x + 10,
                r.y + 25,
                r.width - 20,
                20),
            "Min: " +
            GetMinFPS() +
            "  Max: " +
            maxFPS.ToString("0"),
            small);

        GUI.Label(
            new Rect(
                r.x + 10,
                r.y + 45,
                r.width - 20,
                20),
            "Average: " +
            averageFPS.ToString("0.0"),
            small);
    }

    // =========================================================
    // FPS GRAPH
    // =========================================================

    private void DrawFPSGraph()
    {
        Rect graph =
            new Rect(
                15,
                fpsStatsEnabled
                    ? 350
                    : 265,
                220,
                90);

        GUI.Box(
            graph,
            GUIContent.none);

        for (int i = 1;
             i < fpsHistory.Length;
             i++)
        {
            int a =
                (fpsIndex + i - 1) %
                fpsHistory.Length;

            int b =
                (fpsIndex + i) %
                fpsHistory.Length;

            float x1 =
                graph.x +
                (i - 1f) /
                fpsHistory.Length *
                graph.width;

            float x2 =
                graph.x +
                i /
                (float)fpsHistory.Length *
                graph.width;

            float y1 =
                graph.y +
                graph.height -
                Mathf.Clamp01(
                    fpsHistory[a] /
                    240f) *
                graph.height;

            float y2 =
                graph.y +
                graph.height -
                Mathf.Clamp01(
                    fpsHistory[b] /
                    240f) *
                graph.height;

            DrawLine(
                new Vector2(
                    x1,
                    y1),
                new Vector2(
                    x2,
                    y2),
                2);
        }
    }

    private void DrawLine(
        Vector2 a,
        Vector2 b,
        float width)
    {
        Matrix4x4 old =
            GUI.matrix;

        float angle =
            Mathf.Atan2(
                b.y - a.y,
                b.x - a.x) *
            Mathf.Rad2Deg;

        float length =
            Vector2.Distance(
                a,
                b);

        GUIUtility.RotateAroundPivot(
            angle,
            a);

        GUI.DrawTexture(
            new Rect(
                a.x,
                a.y - width / 2,
                length,
                width),
            Texture2D.whiteTexture);

        GUI.matrix =
            old;
    }

    // =========================================================
    // PLAYER OVERLAY
    // =========================================================

    private void DrawPlayerOverlay()
    {
        Rect r =
            new Rect(
                Screen.width - 350,
                20,
                330,
                260);

        GUI.Box(
            r,
            GUIContent.none);

        GUI.Label(
            new Rect(
                r.x + 12,
                r.y + 8,
                r.width - 24,
                30),
            "Players",
            label);

        GUILayout.BeginArea(
            new Rect(
                r.x + 12,
                r.y + 42,
                r.width - 24,
                r.height - 52));

        foreach (PlayerData p in players)
        {
            GUILayout.Label(
                p.Name +
                " | " +
                p.Color +
                " | " +
                FormatTime(p.Idle),
                small);
        }

        GUILayout.EndArea();
    }

    // =========================================================
    // FPS UPDATE
    // =========================================================

    private void UpdateFPS()
    {
        fpsFrames++;

        fpsTime +=
            Time.unscaledDeltaTime;

        frameTime =
            Time.unscaledDeltaTime *
            1000f;

        if (fpsTime < .5f)
            return;

        fps =
            fpsFrames /
            fpsTime;

        fpsFrames = 0;
        fpsTime = 0;

        fpsHistory[fpsIndex] =
            fps;

        fpsIndex =
            (fpsIndex + 1) %
            fpsHistory.Length;

        if (fps > 0)
        {
            minFPS =
                Mathf.Min(
                    minFPS,
                    fps);

            maxFPS =
                Mathf.Max(
                    maxFPS,
                    fps);

            fpsTotal += fps;
            fpsSamples++;

            averageFPS =
                fpsTotal /
                Mathf.Max(
                    1,
                    fpsSamples);
        }
    }

    // =========================================================
    // HUD CACHE
    // =========================================================

    private void UpdateHUDCache()
    {
        hudTimer +=
            Time.unscaledDeltaTime;

        if (hudTimer < .25f)
            return;

        hudTimer = 0;

        clockText =
            DateTime.Now.ToString(
                "HH:mm:ss");

        runtimeText =
            "Runtime: " +
            TimeSpan
                .FromSeconds(
                    Time.realtimeSinceStartup)
                .ToString(
                    @"hh\:mm\:ss");

        resolutionText =
            Screen.width +
            "x" +
            Screen.height;

        refreshRateText =
            "Refresh Rate: " +
            Screen.currentResolution
                .refreshRate +
            " Hz";

        qualityText =
            "Quality: " +
            QualitySettings
                .names[
                    Mathf.Clamp(
                        QualitySettings
                            .GetQualityLevel(),
                        0,
                        QualitySettings
                            .names.Length - 1)];
    }

    // =========================================================
    // PLAYER UPDATE
    // =========================================================

    private void UpdatePlayerIdle()
    {
        float now =
            Time.unscaledTime;

        foreach (PlayerData p in players)
        {
            p.Idle =
                Mathf.Max(
                    0,
                    now -
                    p.LastUpdate);
        }
    }

    private void RefreshPlayers()
    {
        float now =
            Time.unscaledTime;

        foreach (PlayerData p in players)
        {
            p.LastUpdate = now;
            p.Idle = 0;
        }

        Notify(
            "Players refreshed.");
    }

    // =========================================================
    // HUD POSITION
    // =========================================================

    private void MoveHUD(
        float x,
        float y)
    {
        Vector2 delta =
            new Vector2(
                x,
                y);

        fpsRect.position += delta;
        clockRect.position += delta;
        runtimeRect.position += delta;
        resolutionRect.position += delta;
        frameTimeRect.position += delta;
    }

    private void ResetHUD()
    {
        fpsRect =
            new Rect(
                15,
                15,
                220,
                28);

        clockRect =
            new Rect(
                15,
                45,
                220,
                28);

        runtimeRect =
            new Rect(
                15,
                75,
                260,
                28);

        resolutionRect =
            new Rect(
                15,
                105,
                260,
                28);

        frameTimeRect =
            new Rect(
                15,
                135,
                260,
                28);

        Notify(
            "HUD reset.");
    }

    // =========================================================
    // PROFILES
    // =========================================================

    private void LoadProfile(
        string profile)
    {
        switch (profile)
        {
            case "Default":
                ApplyDefaultProfile();
                break;

            case "Minimal":
                ApplyMinimalProfile();
                break;

            case "Performance":
                ApplyPerformanceProfile();
                break;

            case "Streamer":
                ApplyStreamerProfile();
                break;
        }

        SaveSettings();

        Notify(
            "Loaded profile: " +
            profile);
    }

    private void ApplyDefaultProfile()
    {
        hud = true;
        fpsEnabled = true;
        clockEnabled = true;
        runtimeEnabled = false;
        resolutionEnabled = false;
        graphEnabled = false;
        statusEnabled = true;

        compact = false;
        performanceMode = false;
    }

    private void ApplyMinimalProfile()
    {
        hud = true;
        fpsEnabled = true;
        clockEnabled = true;
        runtimeEnabled = false;
        resolutionEnabled = false;
        graphEnabled = false;
        statusEnabled = false;

        compact = true;
        performanceMode = false;
    }

    private void ApplyPerformanceProfile()
    {
        hud = true;
        fpsEnabled = true;
        clockEnabled = false;
        runtimeEnabled = false;
        resolutionEnabled = false;
        graphEnabled = true;
        statusEnabled = false;

        compact = true;
        performanceMode = true;

        animations = false;
    }

    private void ApplyStreamerProfile()
    {
        hud = true;
        fpsEnabled = true;
        clockEnabled = true;
        runtimeEnabled = true;
        resolutionEnabled = false;
        graphEnabled = true;
        statusEnabled = true;

        compact = false;
        performanceMode = false;
    }

    // =========================================================
    // FAVORITES
    // =========================================================

    private bool IsFavorite(
        string name)
    {
        return favorites.Contains(
            name);
    }

    private void ToggleFavorite(
        string name)
    {
        if (!favorites.Add(name))
            favorites.Remove(name);

        SaveFavorites();
    }

    // =========================================================
    // THEMES
    // =========================================================

    private void ApplyTheme(
        int id)
    {
        switch (id)
        {
            case 0:
                accent =
                    new Color(
                        .35f,
                        .65f,
                        1f);
                break;

            case 1:
                accent =
                    new Color(
                        1f,
                        .35f,
                        .75f);
                break;

            case 2:
                accent =
                    new Color(
                        .25f,
                        .55f,
                        1f);
                break;

            case 3:
                accent =
                    new Color(
                        .65f,
                        .35f,
                        1f);
                break;

            case 4:
                accent =
                    new Color(
                        1f,
                        .25f,
                        .25f);
                break;

            case 5:
                accent =
                    new Color(
                        .25f,
                        1f,
                        .55f);
                break;
        }
    }

    // =========================================================
    // NOTIFICATIONS
    // =========================================================

    private void Notify(
        string message)
    {
        if (!notifications)
            return;

        status = message;

        notificationUntil =
            Time.unscaledTime +
            NotificationDuration;

        notificationHistory.Insert(
            0,
            DateTime.Now.ToString(
                "HH:mm:ss") +
            " - " +
            message);

        while (notificationHistory.Count > 25)
        {
            notificationHistory.RemoveAt(
                notificationHistory.Count - 1);
        }
    }

    private void UpdateNotification()
    {
        if (!string.IsNullOrEmpty(status) &&
            Time.unscaledTime >=
            notificationUntil)
        {
            status = "";
        }
    }

    private void DrawNotifications()
    {
        if (!notifications ||
            string.IsNullOrEmpty(status))
        {
            return;
        }

        Rect r =
            new Rect(
                Screen.width - 330,
                Screen.height - 70,
                310,
                45);

        GUI.Box(
            r,
            GUIContent.none);

        GUI.Label(
            new Rect(
                r.x + 14,
                r.y + 6,
                r.width - 28,
                r.height - 12),
            status,
            small);
    }

    // =========================================================
    // SETTINGS
    // =========================================================

    private void SaveSettings()
    {
        PlayerPrefs.SetString(
            Prefix + "Key",
            menuKey.ToString());

        PlayerPrefs.SetFloat(
            Prefix + "Opacity",
            opacity);

        PlayerPrefs.SetFloat(
            Prefix + "Scale",
            scale);

        PlayerPrefs.SetInt(
            Prefix + "Animations",
            animations ? 1 : 0);

        PlayerPrefs.SetInt(
            Prefix + "Notifications",
            notifications ? 1 : 0);

        PlayerPrefs.SetInt(
            Prefix + "Compact",
            compact ? 1 : 0);

        PlayerPrefs.SetInt(
            Prefix + "Theme",
            theme);

        PlayerPrefs.SetInt(
            Prefix + "Performance",
            performanceMode ? 1 : 0);

        SaveWindow();
        SaveFavorites();

        PlayerPrefs.Save();
    }

    private void LoadSettings()
    {
        string key =
            PlayerPrefs.GetString(
                Prefix + "Key",
                "Delete");

        if (Enum.TryParse(
            key,
            out KeyCode parsed))
        {
            menuKey = parsed;
        }

        opacity =
            PlayerPrefs.GetFloat(
                Prefix + "Opacity",
                1f);

        scale =
            PlayerPrefs.GetFloat(
                Prefix + "Scale",
                1f);

        animations =
            PlayerPrefs.GetInt(
                Prefix + "Animations",
                1) == 1;

        notifications =
            PlayerPrefs.GetInt(
                Prefix + "Notifications",
                1) == 1;

        compact =
            PlayerPrefs.GetInt(
                Prefix + "Compact",
                0) == 1;

        theme =
            PlayerPrefs.GetInt(
                Prefix + "Theme",
                0);

        performanceMode =
            PlayerPrefs.GetInt(
                Prefix + "Performance",
                0) == 1;

        LoadWindow();
        LoadFavorites();

        ApplyTheme(theme);
    }

    private void SaveWindow()
    {
        PlayerPrefs.SetFloat(
            Prefix + "WindowX",
            window.x);

        PlayerPrefs.SetFloat(
            Prefix + "WindowY",
            window.y);

        PlayerPrefs.SetFloat(
            Prefix + "WindowW",
            window.width);

        PlayerPrefs.SetFloat(
            Prefix + "WindowH",
            window.height);
    }

    private void LoadWindow()
    {
        window.x =
            PlayerPrefs.GetFloat(
                Prefix + "WindowX",
                250);

        window.y =
            PlayerPrefs.GetFloat(
                Prefix + "WindowY",
                120);

        window.width =
            PlayerPrefs.GetFloat(
                Prefix + "WindowW",
                780);

        window.height =
            PlayerPrefs.GetFloat(
                Prefix + "WindowH",
                590);

        ClampWindow();
    }

    private void SaveFavorites()
    {
        string value =
            string.Join(
                "|",
                favorites.ToArray());

        PlayerPrefs.SetString(
            Prefix + "Favorites",
            value);
    }

    private void LoadFavorites()
    {
        favorites.Clear();

        string value =
            PlayerPrefs.GetString(
                Prefix + "Favorites",
                "");

        if (string.IsNullOrEmpty(value))
            return;

        string[] split =
            value.Split('|');

        foreach (string item in split)
        {
            if (!string.IsNullOrEmpty(item))
                favorites.Add(item);
        }
    }

    // =========================================================
    // RESET
    // =========================================================

    private void ResetUI()
    {
        opacity = 1f;
        scale = 1f;
        animations = true;
        compact = false;
        performanceMode = false;
        theme = 0;

        ApplyTheme(theme);

        SaveSettings();

        Notify(
            "UI settings reset.");
    }

    private void ResetEverything()
    {
        opacity = 1f;
        scale = 1f;

        animations = true;
        notifications = true;
        compact = false;
        debug = false;
        performanceMode = false;

        menuKey = KeyCode.Delete;

        theme = 0;

        ApplyTheme(theme);
        ResetHUD();

        window =
            new Rect(
                250,
                120,
                780,
                590);

        maximized = false;

        favorites.Clear();

        SaveSettings();

        Notify(
            "Everything reset.");
    }

    // =========================================================
    // HELPERS
    // =========================================================

    private void Header(
        string text)
    {
        GUILayout.Label(
            text,
            title);

        GUILayout.Space(8);
    }

    private void Space()
    {
        GUILayout.Space(8);
    }

    private bool Toggle(
        bool value,
        string text,
        string id)
    {
        if (!MatchesSearch(
            text,
            id))
        {
            return value;
        }

        return GUILayout.Toggle(
            value,
            text,
            toggleStyle);
    }

    private void Button(
        string text,
        Action action)
    {
        if (!MatchesSearch(
            text,
            text))
        {
            return;
        }

        if (GUILayout.Button(
            text,
            buttonStyle,
            GUILayout.Height(34)))
        {
            action?.Invoke();
        }
    }

    private bool MatchesSearch(
        string text,
        string id)
    {
        if (favoritesOnly &&
            !favorites.Contains(id))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(search))
            return true;

        return
            text.IndexOf(
                search,
                StringComparison
                    .OrdinalIgnoreCase) >= 0 ||
            id.IndexOf(
                search,
                StringComparison
                    .OrdinalIgnoreCase) >= 0;
    }

    private float Ease(
        float x)
    {
        x =
            Mathf.Clamp01(x);

        return 1f -
            Mathf.Pow(
                1f - x,
                3f);
    }

    private string FormatTime(
        float seconds)
    {
        return TimeSpan
            .FromSeconds(
                Mathf.Max(
                    0,
                    seconds))
            .ToString(
                @"mm\:ss");
    }

    private string GetMinFPS()
    {
        if (minFPS == float.MaxValue)
            return "N/A";

        return minFPS.ToString("0");
    }

    private string GetSceneName()
    {
        try
        {
            return
                UnityEngine.SceneManagement
                    .SceneManager
                    .GetActiveScene()
                    .name;
        }
        catch
        {
            return "Unknown";
        }
    }

    // =========================================================
    // PLAYER DATA
    // =========================================================

    private sealed class PlayerData
    {
        public string Name;
        public string Color;
        public string Device;
        public string Status;

        public float LastUpdate;
        public float Idle;

        public PlayerData(
            string name,
            string color,
            string device)
        {
            Name = name;
            Color = color;
            Device = device;
            Status = "Active";

            LastUpdate =
                Time.unscaledTime;
        }
    }
}
