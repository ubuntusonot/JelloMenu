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

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F4))
        {
            menuOpen = !menuOpen;
        }
    }

    private void OnGUI()
    {
        if (!menuOpen)
            return;

        CreateStyles();

        // Menu background
        GUI.Box(
            new Rect(250, 150, 420, 280),
            ""
        );

        // Title
        GUI.Label(
            new Rect(270, 175, 380, 40),
            "JELLO",
            titleStyle
        );

        // Status
        GUI.Label(
            new Rect(270, 215, 380, 25),
            "QOL Menu Loaded",
            labelStyle
        );

        // Test button
        if (GUI.Button(
            new Rect(290, 255, 340, 40),
            "Test Button",
            buttonStyle
        ))
        {
            Debug.Log("Smart Looking At Logs");
        }

        // Close button
        if (GUI.Button(
            new Rect(290, 310, 340, 40),
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
