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
    private bool menuOpen = false;
    private bool waitingForKey = false;

    private KeyCode menuKey = KeyCode.Delete;

    private Rect window = new Rect(250, 150, 440, 400);

    private GUIStyle? titleStyle;
    private GUIStyle? buttonStyle;
    private GUIStyle? labelStyle;

    private string statusText = "Jello Loaded";

    // HUD features
    private bool showFPS = true;
    private bool showClock = true;
    private bool hudEditMode = false;

    // UI settings
    private float menuOpacity = 1.0f;
    private float uiScale = 1.0f;

    // FPS
    private float fps = 0f;
    private float fpsTimer = 0f;
    private int fpsFrames = 0;

    // HUD positions
    private Rect fpsRect = new Rect(15, 15, 180, 30);
    private Rect clockRect = new Rect(15, 45, 180, 30);

    private void Start()
    {
        // Load saved keybind.
        string savedKey = PlayerPrefs.GetString(
            "Jello_MenuKey",
            "F4"
        );

        if (Enum.TryParse(savedKey, out KeyCode loadedKey))
            menuKey = loadedKey;
    }

    private void Update()
    {
        // FPS calculation.
        fpsFrames++;
        fpsTimer += Time.unscaledDeltaTime;

        if (fpsTimer >= 0.5f)
        {
            fps = fpsFrames / fpsTimer;
            fpsFrames = 0;
            fpsTimer = 0f;
        }
    }

    private void OnGUI()
    {
        // Menu keybind.
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

        DrawHUD();

        if (!menuOpen)
            return;

        CreateStyles();

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
            (GUI.WindowFunction)((id) => DrawMenu(id)),
            "Jello"
        );

        GUI.matrix = oldMatrix;
        GUI.color = oldColor;
    }

    private void DrawHUD()
    {
        if (showFPS)
        {
            GUI.Label(
                fpsRect,
                $"FPS: {fps:0}"
            );

            if (hudEditMode)
                GUI.Box(fpsRect, "");
        }

        if (showClock)
        {
            GUI.Label(
                clockRect,
                DateTime.Now.ToString("HH:mm:ss")
            );

            if (hudEditMode)
                GUI.Box(clockRect, "");
        }

        // Allow HUD elements to be dragged.
        if (hudEditMode &&
            Event.current != null &&
            Event.current.type == EventType.MouseDrag &&
            Event.current.button == 0)
        {
            Vector2 mouse = Event.current.mousePosition;

            if (fpsRect.Contains(mouse))
            {
                fpsRect.position += Event.current.delta;
                Event.current.Use();
            }
            else if (clockRect.Contains(mouse))
            {
                clockRect.position += Event.current.delta;
                Event.current.Use();
            }
        }
    }

    private void DrawMenu(int id)
    {
        GUILayout.Space(10);

        GUILayout.Label(
            "JELLO",
            titleStyle
        );

        GUILayout.Space(10);

        GUILayout.Label(
            statusText,
            labelStyle
        );

        GUILayout.Space(10);

        // HUD toggles.
        showFPS = GUILayout.Toggle(
            showFPS,
            "Show FPS"
        );

        showClock = GUILayout.Toggle(
            showClock,
            "Show Clock"
        );

        hudEditMode = GUILayout.Toggle(
            hudEditMode,
            "Edit HUD"
        );

        GUILayout.Space(10);

        // Opacity.
        GUILayout.Label(
            $"Menu Opacity: {menuOpacity:0.00}",
            labelStyle
        );

        menuOpacity = GUILayout.HorizontalSlider(
            menuOpacity,
            0.25f,
            1.0f
        );

        // Scale.
        GUILayout.Label(
            $"UI Scale: {uiScale:0.00}",
            labelStyle
        );

        uiScale = GUILayout.HorizontalSlider(
            uiScale,
            0.75f,
            1.5f
        );

        GUILayout.Space(10);

        // Keybind section.
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
                KeyCode newKey = Event.current.keyCode;

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
            menuKey = KeyCode.F4;

            PlayerPrefs.SetString(
                "Jello_MenuKey",
                "F4"
            );

            PlayerPrefs.Save();

            waitingForKey = false;

            statusText = "Keybind reset to F4.";
        }

        GUILayout.Space(10);

        // Test.
        if (GUILayout.Button(
            "Test Button",
            GUILayout.Height(35)
        ))
        {
            statusText = "Button works!";

            Debug.Log(
                "[Jello] Test Button Works!"
            );
        }

        // Reset HUD.
        if (GUILayout.Button(
            "Reset HUD",
            GUILayout.Height(30)
        ))
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

            statusText = "HUD reset.";
        }

        // Close.
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

    private void CreateStyles()
    {
        if (titleStyle != null)
            return;

        titleStyle = new GUIStyle(
            GUI.skin.label
        )
        {
            fontSize = 24,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };

        labelStyle = new GUIStyle(
            GUI.skin.label
        )
        {
            fontSize = 14,
            alignment = TextAnchor.MiddleCenter
        };

        buttonStyle = new GUIStyle(
            GUI.skin.button
        )
        {
            fontSize = 14
        };
    }
}
