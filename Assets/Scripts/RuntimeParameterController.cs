using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 実行時にパラメータをGUI上で調整できるコントローラー
/// </summary>
public class RuntimeParameterController : MonoBehaviour
{
    [Header("参照")]
    public NormLowSequenceDetector detector;
    public OSCManager oscManager;

    [Header("GUI設定")]
    [Tooltip("GUIを表示するか")]
    public bool showGUI = true;

    [Tooltip("GUIのスケール（1.0 = 100%）")]
    public float guiScale = 1.0f;

    // GUI表示用の内部状態
    private bool showDepthSettings = true;
    private bool showDetectionSettings = true;
    private bool showRegionSettings = false;
    private bool showOSCSettings = true;

    private Vector2 scrollPosition = Vector2.zero;
    private int selectedRegion = 0;

    // 文字列入力用の一時変数
    private string oscHostInput = "";
    private string oscPortInput = "";

    void Start()
    {
        if (detector == null)
        {
            detector = FindObjectOfType<NormLowSequenceDetector>();
        }

        if (oscManager == null)
        {
            oscManager = FindObjectOfType<OSCManager>();
        }

        // OSC設定の初期値を取得
        if (oscManager != null)
        {
            oscHostInput = oscManager.RemoteHost;
            oscPortInput = oscManager.RemotePort.ToString();
        }
    }

    void OnGUI()
    {
        if (!showGUI) return;

        // GUIスケールの適用
        Matrix4x4 originalMatrix = GUI.matrix;
        GUI.matrix = Matrix4x4.Scale(new Vector3(guiScale, guiScale, 1.0f));

        // メインウィンドウ
        GUILayout.BeginArea(new Rect(10, 10, 400 / guiScale, Screen.height / guiScale - 20));

        scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Width(400 / guiScale), GUILayout.Height(Screen.height / guiScale - 20));

        GUILayout.Label("=== ランタイムパラメータコントローラー ===", GUI.skin.box);

        // GUIスケール設定
        DrawGUIScaleSettings();

        // 深度設定
        DrawDepthSettings();

        // 検知設定
        DrawDetectionSettings();

        // 領域設定
        DrawRegionSettings();

        // OSC設定
        DrawOSCSettings();

        GUILayout.EndScrollView();
        GUILayout.EndArea();

        // 元のマトリックスに戻す
        GUI.matrix = originalMatrix;
    }

    /// <summary>
    /// GUIスケール設定
    /// </summary>
    private void DrawGUIScaleSettings()
    {
        GUILayout.BeginVertical(GUI.skin.box);

        GUILayout.Label($"GUI Scale: {guiScale:F1}x (文字サイズ調整)", GUI.skin.box);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("0.8x", GUILayout.Width(60)))
        {
            guiScale = 0.8f;
        }
        if (GUILayout.Button("1.0x", GUILayout.Width(60)))
        {
            guiScale = 1.0f;
        }
        if (GUILayout.Button("1.5x", GUILayout.Width(60)))
        {
            guiScale = 1.5f;
        }
        if (GUILayout.Button("2.0x", GUILayout.Width(60)))
        {
            guiScale = 2.0f;
        }
        if (GUILayout.Button("2.5x", GUILayout.Width(60)))
        {
            guiScale = 2.5f;
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(5);

        guiScale = GUILayout.HorizontalSlider(guiScale, 0.5f, 3.0f);

        GUILayout.EndVertical();

        GUILayout.Space(10);
    }

    /// <summary>
    /// 深度設定のGUI
    /// </summary>
    private void DrawDepthSettings()
    {
        if (detector == null) return;

        showDepthSettings = GUILayout.Toggle(showDepthSettings, "▼ 深度設定 (Depth Settings)", GUI.skin.button);

        if (showDepthSettings)
        {
            GUILayout.BeginVertical(GUI.skin.box);

            GUILayout.Label($"Depth Scale: {detector.depthScale:F1}");
            detector.depthScale = GUILayout.HorizontalSlider(detector.depthScale, 1000f, 5000f);

            GUILayout.Space(5);

            GUILayout.Label($"Min Depth Threshold: {detector.minDepthThreshold:F2} m");
            detector.minDepthThreshold = GUILayout.HorizontalSlider(detector.minDepthThreshold, 0.1f, 2.0f);

            GUILayout.Space(5);

            GUILayout.Label($"Max Depth Threshold: {detector.maxDepthThreshold:F2} m");
            detector.maxDepthThreshold = GUILayout.HorizontalSlider(detector.maxDepthThreshold, 0.5f, 3.0f);

            GUILayout.EndVertical();
        }

        GUILayout.Space(10);
    }

    /// <summary>
    /// 検知設定のGUI
    /// </summary>
    private void DrawDetectionSettings()
    {
        if (detector == null) return;

        showDetectionSettings = GUILayout.Toggle(showDetectionSettings, "▼ 検知設定 (Detection Settings)", GUI.skin.button);

        if (showDetectionSettings)
        {
            GUILayout.BeginVertical(GUI.skin.box);

            GUILayout.Label($"Min Pixels to Trigger: {detector.minNormLowPixelsToTrigger}");
            detector.minNormLowPixelsToTrigger = (int)GUILayout.HorizontalSlider(detector.minNormLowPixelsToTrigger, 10, 200);

            GUILayout.Space(5);

            GUILayout.Label($"Sequence Max Time Gap: {detector.sequenceMaxTimeGap:F2} 秒");
            detector.sequenceMaxTimeGap = GUILayout.HorizontalSlider(detector.sequenceMaxTimeGap, 0.1f, 2.0f);

            GUILayout.EndVertical();
        }

        GUILayout.Space(10);
    }

    /// <summary>
    /// 領域設定のGUI
    /// </summary>
    private void DrawRegionSettings()
    {
        if (detector == null || detector.regions == null || detector.regions.Length == 0) return;

        showRegionSettings = GUILayout.Toggle(showRegionSettings, "▼ 領域設定 (Region Settings)", GUI.skin.button);

        if (showRegionSettings)
        {
            GUILayout.BeginVertical(GUI.skin.box);

            // 領域選択
            GUILayout.Label("編集する領域を選択:");
            GUILayout.BeginHorizontal();
            for (int i = 0; i < detector.regions.Length; i++)
            {
                if (GUILayout.Button($"Region {i}", selectedRegion == i ? GUI.skin.box : GUI.skin.button))
                {
                    selectedRegion = i;
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            // 選択された領域の詳細設定
            if (selectedRegion >= 0 && selectedRegion < detector.regions.Length)
            {
                var region = detector.regions[selectedRegion];

                GUILayout.Label($"=== Region {selectedRegion}: {region.name} ===", GUI.skin.box);

                GUILayout.Label($"X: {region.x}");
                region.x = (int)GUILayout.HorizontalSlider(region.x, 0, 640);

                GUILayout.Label($"Y: {region.y}");
                region.y = (int)GUILayout.HorizontalSlider(region.y, 0, 480);

                GUILayout.Label($"Width: {region.width}");
                region.width = (int)GUILayout.HorizontalSlider(region.width, 10, 300);

                GUILayout.Label($"Height: {region.height}");
                region.height = (int)GUILayout.HorizontalSlider(region.height, 10, 300);

                // 変更を反映
                detector.regions[selectedRegion] = region;
            }

            GUILayout.EndVertical();
        }

        GUILayout.Space(10);
    }

    /// <summary>
    /// OSC設定のGUI
    /// </summary>
    private void DrawOSCSettings()
    {
        if (oscManager == null) return;

        showOSCSettings = GUILayout.Toggle(showOSCSettings, "▼ OSC設定 (OSC Settings)", GUI.skin.button);

        if (showOSCSettings)
        {
            GUILayout.BeginVertical(GUI.skin.box);

            // Remote Host
            GUILayout.Label($"Remote Host: {oscManager.RemoteHost}");
            GUILayout.BeginHorizontal();
            oscHostInput = GUILayout.TextField(oscHostInput, GUILayout.Width(200));
            if (GUILayout.Button("適用", GUILayout.Width(60)))
            {
                oscManager.RemoteHost = oscHostInput;
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(5);

            // Remote Port
            GUILayout.Label($"Remote Port: {oscManager.RemotePort}");
            GUILayout.BeginHorizontal();
            oscPortInput = GUILayout.TextField(oscPortInput, GUILayout.Width(200));
            if (GUILayout.Button("適用", GUILayout.Width(60)))
            {
                if (int.TryParse(oscPortInput, out int port))
                {
                    oscManager.RemotePort = port;
                }
                else
                {
                    Debug.LogWarning("Invalid port number!");
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            // Rate Limiter設定
            GUILayout.Label("=== レート制限設定 ===", GUI.skin.box);

            oscManager.EnableRateLimiter = GUILayout.Toggle(oscManager.EnableRateLimiter, "Enable Rate Limiter");

            GUILayout.Label($"Limit Time: {oscManager.LimitTime:F2} 秒");
            oscManager.LimitTime = GUILayout.HorizontalSlider(oscManager.LimitTime, 0.1f, 3.0f);

            GUILayout.Label($"Limit Count: {oscManager.LimitCount}");
            oscManager.LimitCount = (int)GUILayout.HorizontalSlider(oscManager.LimitCount, 1, 10);

            GUILayout.EndVertical();
        }

        GUILayout.Space(10);
    }

    void Update()
    {
        // HキーでGUIの表示/非表示を切り替え
        if (Input.GetKeyDown(KeyCode.H))
        {
            showGUI = !showGUI;
        }

        // +キーでGUIを拡大
        if (Input.GetKeyDown(KeyCode.Plus) || Input.GetKeyDown(KeyCode.KeypadPlus))
        {
            guiScale = Mathf.Min(3.0f, guiScale + 0.1f);
            Debug.Log($"GUI Scale: {guiScale:F1}x");
        }

        // -キーでGUIを縮小
        if (Input.GetKeyDown(KeyCode.Minus) || Input.GetKeyDown(KeyCode.KeypadMinus))
        {
            guiScale = Mathf.Max(0.5f, guiScale - 0.1f);
            Debug.Log($"GUI Scale: {guiScale:F1}x");
        }
    }
}
