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

    private Rect window = new Rect(250, 150, 420, 280);

    private GUIStyle? titleStyle;
    private GUIStyle? buttonStyle;
    private GUIStyle? labelStyle;

    private string statusText = "Jello Loaded";

    private void OnGUI()
    {
        // F4 keybind
        if (Event.current != null &&
            Event.current.type == EventType.KeyDown &&
            Event.current.keyCode == KeyCode.Delete)
        {
            menuOpen = !menuOpen;

            Debug.Log("[Jello] Keycode pressed! Menu = " + menuOpen);

            Event.current.Use();
        }

        if (!menuOpen)
            return;

        CreateStyles();

        // Background
        GUI.Box(window, "");

        // Title
        GUI.Label(
            new Rect(
                window.x + 20,
                window.y + 15,
                window.width - 40,
                40
            ),
            "JELLO",
            titleStyle
        );

        // Status
        GUI.Label(
            new Rect(
                window.x + 20,
                window.y + 60,
                window.width - 40,
                30
            ),
            statusText,
            labelStyle
        );

        // Test button
        if (GUI.Button(
            new Rect(
                window.x + 40,
                window.y + 105,
                window.width - 80,
                40
            ),
            "Test Button",
            buttonStyle
        ))
        {
            statusText = "Button works!";
            Debug.Log("[Jello] Test Button Works!");
        }

        // Close button
        if (GUI.Button(
            new Rect(
                window.x + 40,
                window.y + 160,
                window.width - 80,
                40
            ),
            "Close",
            buttonStyle
        ))
        {
            menuOpen = false;
            Debug.Log("[Jello] Menu closed.");
        }

        // Drag window
        GUI.DragWindow(
            new Rect(0, 0, window.width, 55)
        );
    }

    private void CreateStyles()
    {
        if (titleStyle != null)
            return;

        titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 24,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };

        labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            alignment = TextAnchor.MiddleCenter
        };

        buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 14
        };
    }
}
