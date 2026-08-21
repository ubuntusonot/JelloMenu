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

    private string statusText = "QOL Menu Loaded";

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F4))
            menuOpen = !menuOpen;
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
            statusText,
            labelStyle
        );

        if (GUI.Button(
            new Rect(window.x + 40, window.y + 105, 340, 40),
            "Send Hello!",
            buttonStyle
        ))
        {
            SendHello();
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

    private void SendHello()
    {
        if (PlayerControl.LocalPlayer == null)
        {
            statusText = "No local player!";
            Debug.Log("[Jello] LocalPlayer is null.");
            return;
        }

        bool sent = PlayerControl.LocalPlayer.RpcSendChat("Hello!");

        if (sent)
        {
            statusText = "Sent: Hello!";
            Debug.Log("[Jello] Sent Hello!");
        }
        else
        {
            statusText = "Chat failed.";
            Debug.Log("[Jello] RpcSendChat returned false.");
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
