using UnityEngine;
using UnityEngine.SceneManagement;

public class DebugMenu : MonoBehaviour
{
    public KeyCode toggleKey = KeyCode.X;
    public KeyCode reloadSceneKey = KeyCode.R;
    public KeyCode toggleVSyncKey = KeyCode.V;
    public KeyCode loadMenuKey = KeyCode.M;

    bool show = true;
    float deltaTime;

    GUIStyle labelStyle;
    GUIStyle headerStyle;

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
            show = !show;

        if (Input.GetKeyDown(reloadSceneKey))
            ReloadScene();

        if (Input.GetKeyDown(toggleVSyncKey))
            ToggleVSync();

        if (Input.GetKeyDown(loadMenuKey))
            LoadMenu();

        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
    }

    void OnGUI()
    {
        if (!show)
            return;

        InitStyles();

        float fps = 1f / deltaTime;
        float ms = deltaTime * 1000f;

        float width = 440;
        float height = 510;
        float padding = 60;

        Rect boxRect = new Rect(
            Screen.width - width - padding,
            padding,
            width,
            height
        );

        // Background
        GUI.color = new Color(0, 0, 0, 0.65f);
        GUI.Box(boxRect, GUIContent.none);

        // Content
        GUI.color = Color.white;
        GUILayout.BeginArea(boxRect);
        GUILayout.Space(16);

        GUILayout.Label("   METRICS", headerStyle);
        GUILayout.Space(10);

        GUILayout.Label($"  FPS: {fps:0}", labelStyle);
        GUILayout.Label($"  Frame Time: {ms:0.00} ms", labelStyle);

        GUILayout.Space(12);

        GUILayout.Label($"  Resolution: {Screen.width} x {Screen.height}", labelStyle);
        GUILayout.Label($"  VSync: {(QualitySettings.vSyncCount > 0 ? "On" : "Off")}", labelStyle);

        GUILayout.Space(18);

        GUILayout.Label("  CONTROLS", headerStyle);
        GUILayout.Space(6);

        GUILayout.Label($"  [{toggleKey}] Hide / Show This Menu", labelStyle);
        GUILayout.Label($"  [{reloadSceneKey}] Reload scene", labelStyle);
        GUILayout.Label($"  [{toggleVSyncKey}] Toggle VSync", labelStyle);
        GUILayout.Label($"  [{loadMenuKey}] Load menu (Scene 0)", labelStyle);

        GUILayout.EndArea();
    }

    void ReloadScene()
    {
        Scene scene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(scene.buildIndex);
    }

    void LoadMenu()
    {
        SceneManager.LoadScene(0);
    }

    void ToggleVSync()
    {
        QualitySettings.vSyncCount = QualitySettings.vSyncCount > 0 ? 0 : 1;
    }

    void InitStyles()
    {
        if (labelStyle != null)
            return;

        labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.fontSize = 28;
        labelStyle.normal.textColor = Color.white;

        headerStyle = new GUIStyle(labelStyle);
        headerStyle.fontSize = 34;
        headerStyle.fontStyle = FontStyle.Bold;
    }
}
