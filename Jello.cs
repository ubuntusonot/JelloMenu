using System;
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

    private Rect window = new Rect(250, 150, 440, 500);

    private GUIStyle? titleStyle;
    private GUIStyle? buttonStyle;
    private GUIStyle? labelStyle;
    private GUIStyle? hudStyle;

    private string statusText = "Jello Loaded";

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
    // UI SETTINGS
    // =========================

    private float menuOpacity = 1.0f;
    private float uiScale = 1.0f;

    private int theme = 0;

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
    // START
    // =========================

    private void Start()
    {
        string savedKey = PlayerPrefs.GetString(
            "Jello_MenuKey",
            "Delete"
        );

        if (Enum.TryParse(
            savedKey,
            out KeyCode loadedKey))
        {
            menuKey = loadedKey;
        }
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

            Debug.Log(
                "[Jello] Menu = " + menuOpen
            );

            Event.current.Use();
        }

        CreateStyles();

        DrawHUD();

        if (!menuOpen)
            return;

        Color oldColor = GUI.color;
        Matrix4x4 oldMatrix = GUI.matrix;

        GUI.color = new Color(
            1f,
            1f,
            1f,
            menuOpacity
        );

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
    // HUD
    // =========================

    private void DrawHUD()
    {
        if (!hudEnabled)
            return;

        Color oldColor = GUI.color;

        if (showFPS)
        {
            GUI.color = GetFPSColor();

            GUI.Label(
                fpsRect,
                $"FPS: {fps:0}",
                hudStyle
            );

            GUI.color = oldColor;

            if (hudEditMode)
                GUI.Box(fpsRect, "");
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

            if (hudEditMode)
                GUI.Box(clockRect, "");
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

            if (hudEditMode)
                GUI.Box(runtimeRect, "");
        }

        if (showResolution)
        {
            GUI.Label(
                resolutionRect,
                $"Resolution: {Screen.width}x{Screen.height}",
                hudStyle
            );

            if (hudEditMode)
                GUI.Box(resolutionRect, "");
        }

        // HUD dragging
        if (hudEditMode &&
            Event.current != null &&
            Event.current.type == EventType.MouseDrag &&
            Event.current.button == 0)
        {
            Vector2 mouse =
                Event.current.mousePosition;

            if (fpsRect.Contains(mouse))
            {
                fpsRect.position +=
                    Event.current.delta;

                Event.current.Use();
            }
            else if (clockRect.Contains(mouse))
            {
                clockRect.position +=
                    Event.current.delta;

                Event.current.Use();
            }
            else if (runtimeRect.Contains(mouse))
            {
                runtimeRect.position +=
                    Event.current.delta;

                Event.current.Use();
            }
            else if (resolutionRect.Contains(mouse))
            {
                resolutionRect.position +=
                    Event.current.delta;

                Event.current.Use();
            }
        }
    }

    // =========================
    // MENU
    // =========================

    private void DrawMenu(int id)
    {
        GUILayout.Space(10);

        GUILayout.Label(
            "JELLO",
            titleStyle
        );

        GUILayout.Label(
            statusText,
            labelStyle
        );

        GUILayout.Space(8);

        // -------------------------
        // HUD
        // -------------------------

        GUILayout.Label(
            "HUD",
            titleStyle
        );

        hudEnabled = GUILayout.Toggle(
            hudEnabled,
            "Enable HUD"
        );

        showFPS = GUILayout.Toggle(
            showFPS,
            "Show FPS"
        );

        showClock = GUILayout.Toggle(
            showClock,
            "Show Clock"
        );

        showRuntime = GUILayout.Toggle(
            showRuntime,
            "Show Runtime"
        );

        showResolution = GUILayout.Toggle(
            showResolution,
            "Show Resolution"
        );

        hudEditMode = GUILayout.Toggle(
            hudEditMode,
            "Edit HUD"
        );

        GUILayout.Space(8);

        // -------------------------
        // FPS
        // -------------------------

        GUILayout.Label(
            $"FPS Warning: {fpsWarning:0}",
            labelStyle
        );

        fpsWarning = GUILayout.HorizontalSlider(
            fpsWarning,
            10f,
            120f
        );

        // -------------------------
        // UI
        // -------------------------

        GUILayout.Label(
            "UI",
            titleStyle
        );

        GUILayout.Label(
            $"Menu Opacity: {menuOpacity:0.00}",
            labelStyle
        );

        menuOpacity =
            GUILayout.HorizontalSlider(
                menuOpacity,
                0.25f,
                1.0f
            );

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

        // -------------------------
        // THEME
        // -------------------------

        GUILayout.Label(
            "Theme",
            titleStyle
        );

        if (GUILayout.Button(
            "Change Theme",
            GUILayout.Height(32)
        ))
        {
            theme++;

            if (theme > 2)
                theme = 0;

            ApplyTheme();

            statusText =
                "Theme changed.";
        }

        // -------------------------
        // KEYBIND
        // -------------------------

        GUILayout.Label(
            "Menu Key: " + menuKey,
            labelStyle
        );

        if (!waitingForKey)
        {
            if (GUILayout.Button(
                "Change Keybind",
                GUILayout.Height(35)
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
                        "Keybind changed to " +
                        menuKey;

                    Event.current.Use();
                }
            }
        }

        if (GUILayout.Button(
            "Reset Keybind",
            GUILayout.Height(30)
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
                "Keybind reset to Delete.";
        }

        GUILayout.Space(8);

        // -------------------------
        // BUTTONS
        // -------------------------

        if (GUILayout.Button(
            "Test Button",
            GUILayout.Height(35)
        ))
        {
            statusText =
                "Button works!";

            Debug.Log(
                "[Jello] Test Button Works!"
            );
        }

        if (GUILayout.Button(
            "Reset HUD",
            GUILayout.Height(30)
        ))
        {
            ResetHUD();

            statusText =
                "HUD reset.";
        }

        if (GUILayout.Button(
            "Reset UI",
            GUILayout.Height(30)
        ))
        {
            menuOpacity = 1.0f;
            uiScale = 1.0f;

            statusText =
                "UI reset.";
        }

        if (GUILayout.Button(
            "Close",
            GUILayout.Height(30)
        ))
        {
            menuOpen = false;
            waitingForKey = false;
        }

        // Drag window.
        GUI.DragWindow(
            new Rect(
                0,
                0,
                window.width,
                55
            )
        );
    }

    // =========================
    // FPS COLOR
    // =========================

    private Color GetFPSColor()
    {
        if (fps >= fpsWarning)
        {
            return Color.green;
        }

        if (fps >= fpsWarning * 0.66f)
        {
            return Color.yellow;
        }

        return Color.red;
    }

    // =========================
    // RESET HUD
    // =========================

    private void ResetHUD()
    {
        fpsRect = new Rect(
            15,
            15,
            180,
            30
        );

        clockRect = new Rect(
            15,
            45,
            180,
            30
        );

        runtimeRect = new Rect(
            15,
            75,
            220,
            30
        );

        resolutionRect = new Rect(
            15,
            105,
            250,
            30
        );
    }

    // =========================
    // THEME
    // =========================

    private void ApplyTheme()
    {
        if (titleStyle == null ||
            labelStyle == null ||
            buttonStyle == null)
        {
            return;
        }

        if (theme == 0)
        {
            // Default
            titleStyle.normal.textColor =
                Color.white;

            labelStyle.normal.textColor =
                Color.white;

            buttonStyle.normal.textColor =
                Color.white;
        }
        else if (theme == 1)
        {
            // Jello Green
            titleStyle.normal.textColor =
                new Color(
                    0.3f,
                    1f,
                    0.5f
                );

            labelStyle.normal.textColor =
                new Color(
                    0.8f,
                    1f,
                    0.85f
                );

            buttonStyle.normal.textColor =
                new Color(
                    0.5f,
                    1f,
                    0.6f
                );
        }
        else
        {
            // Cyan
            titleStyle.normal.textColor =
                Color.cyan;

            labelStyle.normal.textColor =
                new Color(
                    0.8f,
                    0.95f,
                    1f
                );

            buttonStyle.normal.textColor =
                Color.cyan;
        }
    }

    // =========================
    // STYLES
    // =========================

    private void CreateStyles()
    {
        if (titleStyle != null)
            return;

        titleStyle = new GUIStyle(
            GUI.skin.label
        )
        {
            fontSize = 20,
            fontStyle = FontStyle.Bold,
            alignment =
                TextAnchor.MiddleCenter
        };

        labelStyle = new GUIStyle(
            GUI.skin.label
        )
        {
            fontSize = 14,
            alignment =
                TextAnchor.MiddleCenter
        };

        hudStyle = new GUIStyle(
            GUI.skin.label
        )
        {
            fontSize = 16,
            fontStyle = FontStyle.Bold
        };

        buttonStyle = new GUIStyle(
            GUI.skin.button
        )
        {
            fontSize = 14
        };

        ApplyTheme();
    }
}
