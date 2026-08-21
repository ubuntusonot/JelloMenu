using BepInEx;
using BepInEx.Unity.IL2CPP;
using UnityEngine;

[BepInPlugin("org.vinegar.gfr", "Jello", "1.0.0")]
public class QOLMenuPlugin : BasePlugin
{
    private bool menuOpen;

    public override void Load()
    {
        Log.LogInfo("Do You Even Read The Logs?");

        AddComponent<QOLMenuUI>();
    }

    private class QOLMenuUI : MonoBehaviour
    {
        private bool open;
        private Rect window = new Rect(250, 150, 400, 300);

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F4))
                open = !open;
        }

        private void OnGUI()
        {
            if (!open)
                return;

            window = GUI.Window(
                12345,
                window,
                DrawMenu,
                "Jello"
            );
        }

        private void DrawMenu(int id)
        {
            GUILayout.Space(15);

            GUILayout.Label(
                "Jello",
                new GUIStyle(GUI.skin.label)
                {
                    fontSize = 22,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                }
            );

            GUILayout.Space(20);

            if (GUILayout.Button("Test Button", GUILayout.Height(40)))
            {
                Debug.Log("Smart Looking At Logs");
            }

            GUILayout.Space(10);

            if (GUILayout.Button("Close", GUILayout.Height(35)))
                open = false;

            GUI.DragWindow();
        }
    }
}
