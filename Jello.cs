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

    private GUIStyle? titleStyle;
    private GUIStyle? buttonStyle;
    private GUIStyle? labelStyle;

    private Rect window = new Rect(250, 150, 420, 280);

    private bool dragging;
    private Vector2 dragOffset;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F4))
            menuOpen = !menuOpen;

        if (!menuOpen)
            return;

        Vector2 mouse = Event.current != null
            ? Event.current.mousePosition
            : Vector2.zero;

        // Start dragging when clicking the title area
        if (Input.GetMouseButtonDown(0) &&
            window.Contains(mouse) &&
            mouse.y >= window.y &&
            mouse.y <= window.y + 40)
        {
            dragging = true;
            dragOffset = mouse - new Vector2(window.x, window.y);
        }

        if (Input.GetMouseButtonUp(0))
            dragging = false;

        if (dragging && Input.GetMouseButton(0))
        {
            window.x = mouse.x - dragOffset.x;
            window.y = mouse.y - dragOffset.y;
        }
    }

    private void OnGUI()
    {
        if (!menuOpen)
            return;

        CreateStyles();

        GUI.Box(window, "");

        GUI.Label(
            new Rect(window.x + 20, window.y + 20, 380, 40),
            "JELLO",
            titleStyle
        );

        GUI.Label(
            new Rect(window.x + 20, window.y + 60, 380, 25),
            "QOL Menu Loaded",
            labelStyle
        );

        if (GUI.Button(
            new Rect(window.x + 40, window.y + 105, 340, 40),
            "Test Button",
            buttonStyle
        ))
        {
            Debug.Log("Smart Looking At Logs");
        }

        if (GUI.Button(
            new Rect(window.x + 40, window.y + 160, 340, 40),
            "Close Menu",
            buttonStyle
        ))
        {
            menuOpen = false;
        }
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
