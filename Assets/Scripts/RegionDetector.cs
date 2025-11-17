using System.Collections.Generic;
using Orbbec;
using OrbbecUnity;
using UnityEngine;
using System;
using System.IO;            


public class NormLowSequenceDetector : MonoBehaviour
{
    // --- 参照 ---
    public OrbbecFrameSource frameSource;
    [Header("OSC Output")]
    public OSCManager oscManager;

    // --- Depth Settings (Physical Range) ---
    [Header("Depth Settings (Physical Range)")]
    [Tooltip("深度のスケールファクター (例: 2700f)")]
    public float depthScale = 2700f; 
    [Tooltip("深度の最小閾値 (メートル)")]
    public float minDepthThreshold = 0.5f; 
    [Tooltip("深度の最大閾値 (メートル)")]
    public float maxDepthThreshold = 1.28f; 
    
    // --- Normalized Range Settings (0.0 to 1.0) ---
    [Header("Normalized Depth Range (0.0 to 1.0)")]
    
    [Tooltip("NormLowの正規化範囲 (0.0 は minDepthThreshold, 1.0 は maxDepthThreshold に対応)")]
    [Range(0f, 1f)] public float normLowStart = 0.1f; // 💡 Inspectorで調整可能
    [Range(0f, 1f)] public float normLowEnd = 0.3f;   // 💡 Inspectorで調整可能

    [Space(10)]
    [Tooltip("Color Band 1 (High Depth) の正規化範囲")]
    [Range(0f, 1f)] public float normHighStart = 0.7f; // 💡 追加: High Depth Start
    [Range(0f, 1f)] public float normHighEnd = 0.96f;  // 💡 追加: High Depth End

    // --- Display Utility ---
    [Header("Display Utility for Height/Depth Mapping")]
    [Tooltip("正規化値をメートルに変換する際に使用する最大の高さ/深さ (例: 3.0f)。この値は検出ロジックには影響しません。")]
    public float maxDisplayHeight = 3.0f; // 💡 ユーザーが入力する「高さの値」

    [Tooltip("正規化値 (0.0-1.0) を、maxDisplayHeightを基準としたメートル値に換算した範囲")]
    public string NormLowDisplayRange
    {
        get
        {
            // D = H_Max - (N * H_Max) の計算
            float startM = maxDisplayHeight - (normLowStart * maxDisplayHeight);
            float endM = maxDisplayHeight - (normLowEnd * maxDisplayHeight);
            
            // 小さい方を先に表示
            return $"NormLow (H={maxDisplayHeight:F1}m): {Mathf.Min(startM, endM):F2}m to {Mathf.Max(startM, endM):F2}m";
        }
    }
    
    [Tooltip("正規化値 (0.0-1.0) を、maxDisplayHeightを基準としたメートル値に換算した範囲")]
    public string NormHighDisplayRange
    {
        get
        {
            // D = H_Max - (N * H_Max) の計算
            float startM = maxDisplayHeight - (normHighStart * maxDisplayHeight);
            float endM = maxDisplayHeight - (normHighEnd * maxDisplayHeight);

            // 小さい方を先に表示
            return $"NormHigh (H={maxDisplayHeight:F1}m): {Mathf.Min(startM, endM):F2}m to {Mathf.Max(startM, endM):F2}m";
        }
    }

    [Header("Detection Settings")]
    [Tooltip("NormLowと判定されるために必要な最小ピクセル数")]
    public int minNormLowPixelsToTrigger = 50; 

    [Tooltip("シーケンス検知の最大時間差 (秒)")]
    public float sequenceMaxTimeGap = 0.5f;
    
    // 監視対象の矩形範囲を定義する構造体
    [System.Serializable]
    public struct DepthRegion
    {
        public string name;
        [Tooltip("領域の左上のX座標 (ピクセル)")]
        public int x;
        [Tooltip("領域の左上のY座標 (ピクセル)")]
        public int y;
        [Tooltip("領域の幅 (ピクセル)")]
        public int width;
        [Tooltip("領域の高さ (ピクセル)")]
        public int height;
        [Tooltip("検知された時に使用する色")] 
        public Color detectedColor;             
        [HideInInspector]
        public int index; // 領域のインデックス（0〜3）
    }

    // 監視する領域
    public DepthRegion[] regions = new DepthRegion[4]
    {
        new DepthRegion { name = "Region 0 (Top-Left)", x = 100, y = 100, width = 50, height = 50, index = 0, detectedColor = Color.red },
        new DepthRegion { name = "Region 1 (Top-Right)", x = 490, y = 100, width = 50, height = 50, index = 1, detectedColor = Color.green },
        new DepthRegion { name = "Region 2 (Bottom-Left)", x = 100, y = 330, width = 50, height = 50, index = 2, detectedColor = Color.blue },
        new DepthRegion { name = "Region 3 (Bottom-Right)", x = 490, y = 330, width = 50, height = 50, index = 3, detectedColor = Color.yellow }
    };
    
    // --- シーケンス追跡用内部状態 ---
    private int _depthWidth = 0;
    private int _depthHeight = 0;
    
    private int _sequenceStartArea = -1;    // シーケンス開始領域のインデックス
    private float _sequenceStartTime = 0f;  // シーケンス開始時刻

    // シーケンス定義マップ: (開始領域, 終了領域, /paddle 番号)
    private readonly (int start, int end, int paddleNum)[] _sequences = {
        (2, 3, 1), // 領域2 -> 領域3 : /paddle 1 右前進
        (0, 1, 2), // 領域0 -> 領域1 : /paddle 2 左前進　
        (3, 2, 3), // 領域3 -> 領域2 : /paddle 3 右後進
        (1, 0, 4)  // 領域1 -> 領域0 : /paddle 4 左後進
    };

    void Start()
    {
        if (oscManager != null)
        {
            oscManager.init();
        }
    }

    // --- OSC通知メソッド (OSCManagerを経由) ----
    public void SendPaddleOSC(int paddleNumber)    
    {
        if (oscManager != null)
        {
            oscManager.SendPaddleOSC(paddleNumber);
        }
    }

    void Update()
    {
        var obDepthFrame = frameSource.GetDepthFrame();

        if (obDepthFrame == null || obDepthFrame.width == 0 || obDepthFrame.height == 0 || obDepthFrame.data == null)
        {
            return;
        }
        if (obDepthFrame.frameType != FrameType.OB_FRAME_DEPTH)
        {
            return;
        }

        // フレームサイズの更新
        if (obDepthFrame.width != _depthWidth || obDepthFrame.height != _depthHeight)
        {
            _depthWidth = obDepthFrame.width;
            _depthHeight = obDepthFrame.height;
        }

        // どの領域がNormLowでアクティブかを判定
        bool[] isRegionActive = new bool[regions.Length];
        List<int> currentActiveRegions = new List<int>();

        for (int i = 0; i < regions.Length; i++)
        {
            int normLowCount = CountNormLowPixelsInRegion(ref regions[i], obDepthFrame.data, _depthWidth, _depthHeight);
            
            if (normLowCount >= minNormLowPixelsToTrigger)
            {
                isRegionActive[i] = true;
                currentActiveRegions.Add(i);
            }
        }

        // シーケンス検知ロジックを実行
        CheckNormLowSequence(currentActiveRegions.ToArray());
    }

    /// ====================================================================================================================
    /// 特定の領域内のNormLow条件を満たすピクセル数をカウントする
    /// ====================================================================================================================
    private int CountNormLowPixelsInRegion(ref DepthRegion region, byte[] depthData, int frameWidth, int frameHeight)
    {
        int normLowCount = 0;
        
        // 領域の範囲をフレーム内にクランプ
        int startX = Mathf.Max(0, region.x);
        int startY = Mathf.Max(0, region.y);
        int endX = Mathf.Min(frameWidth, region.x + region.width);
        int endY = Mathf.Min(frameHeight, region.y + region.height);

        // 深度範囲を動的に計算 (maxDepthThreshold - minDepthThreshold)
        float DEPTH_RANGE = maxDepthThreshold - minDepthThreshold;

        for (int y = startY; y < endY; y++)
        {
            for (int x = startX; x < endX; x++)
            {
                int dataIndex = (y * frameWidth + x) * 2;
                
                // 1. 16ビット深度値の取得
                // データのインデックスチェック（範囲外アクセス防止）
                if (dataIndex + 1 >= depthData.Length) continue;

                ushort depthValue = (ushort)(depthData[dataIndex + 1] << 8 | depthData[dataIndex]);
                
                // 2. 実際の深度計算 (メートル)
                float depth = (float)depthValue / depthScale;

                // 3. 深度が有効範囲内かどうか (depthValue == 0 は通常無効)
                if (depthValue == 0 || depth < minDepthThreshold || depth > maxDepthThreshold)
                {
                    continue; 
                }
                
                // 4. 深度を正規化 (0.0 から 1.0 の範囲)
                float normalizedDepth = (DEPTH_RANGE != 0) ? (depth - minDepthThreshold) / DEPTH_RANGE : 0f;

                // 5. NormLow範囲のチェック
                if (normalizedDepth >= normLowStart && normalizedDepth <= normLowEnd) // 💡 公開フィールドを使用
                {
                    normLowCount++;
                }
            }
        }

        return normLowCount;
    }

    /// ====================================================================================================================
    /// NormLow検出に基づくシーケンス検知ロジック
    /// ====================================================================================================================
    private void CheckNormLowSequence(int[] activeRegions)
    {
        // 1. 時間切れのチェック
        if (_sequenceStartArea != -1 && Time.time > _sequenceStartTime + sequenceMaxTimeGap)
        {
            Debug.Log($"Sequence timed out. Resetting tracking.");
            _sequenceStartArea = -1;
            _sequenceStartTime = 0f;
        }

        // 2. 現在アクティブな領域を処理
        foreach (int currentArea in activeRegions)
        {
            // A. シーケンス追跡中でない場合 (追跡の起点を探す)
            if (_sequenceStartArea == -1)
            {
                // 現在アクティブな領域が、いずれかのシーケンスの開始点と一致するかチェック
                foreach (var seq in _sequences)
                {
                    if (seq.start == currentArea)
                    {
                        _sequenceStartArea = currentArea;
                        _sequenceStartTime = Time.time;
                        Debug.Log($"Sequence tracking started at Region {currentArea}.");
                        return;
                    }
                }
            }
            // B. シーケンス追跡中の場合 (終了エリアを待っている)
            else if (currentArea != _sequenceStartArea)
            {
                // 現在アクティブな領域が、追跡中のシーケンスの終了点と一致するかチェック
                foreach (var seq in _sequences)
                {
                    if (seq.start == _sequenceStartArea && seq.end == currentArea)
                    {
                        // シーケンス成功！
                        SendPaddleOSC(seq.paddleNum);
                        Debug.Log($"SUCCESS: Sequence {seq.start} -> {seq.end} detected. Sending /paddle {seq.paddleNum}.");

                        // 最終通知後、追跡を完全にリセット
                        _sequenceStartArea = -1;
                        _sequenceStartTime = 0f;
                        return;
                    }
                }
            }
        }
    }


    // ==========================================================
    // Gizmos表示ロジック
    // ==========================================================
    private void OnDrawGizmos()
    {
        float W = (_depthWidth > 0) ? _depthWidth : 640;
        float H = (_depthHeight > 0) ? _depthHeight : 480;

        Matrix4x4 oldGizmosMatrix = Gizmos.matrix;
        Gizmos.matrix = transform.localToWorldMatrix * Matrix4x4.Translate(new Vector3(-W / 2f, -H / 2f, 0));
        
        // 外枠
        Gizmos.color = new Color(0.8f, 0.8f, 0.8f, 0.5f);
        Gizmos.DrawWireCube(new Vector3(W / 2, H / 2, 0), new Vector3(W, H, 0));

        // 4つの矩形領域を描画
        for (int i = 0; i < regions.Length; i++)
        {
            DepthRegion region = regions[i];
            
            Vector3 center = new Vector3(region.x + region.width / 2f, H - (region.y + region.height / 2f), 0);
            Vector3 size = new Vector3(region.width, region.height, 0);

            // シーケンス追跡中の領域は設定されたdetectedColorで点滅させる
            if (Application.isPlaying && _sequenceStartArea == i)
            {
                // 追跡開始エリア: 設定されたdetectedColorで点滅
                float pulse = Mathf.Sin(Time.time * 8f) * 0.5f + 0.5f;
                // ここでregion.detectedColorを使用
                Gizmos.color = Color.Lerp(Color.white, region.detectedColor, pulse); 
                Gizmos.DrawCube(center, size);
                Gizmos.color = Color.white; // ワイヤーフレームの色は白に戻すか、別の色にする
                Gizmos.DrawWireCube(center, size);
            }
            else
            {
                // 通常の描画: 青色のワイヤーフレーム
                Gizmos.color = new Color(0f, 0.5f, 1f, 0.8f);
                Gizmos.DrawWireCube(center, size);
            }
            
            // 矩形番号の表示 (Sceneビュー上)
            #if UNITY_EDITOR
            if (!Application.isPlaying || _sequenceStartArea == i)
            {
                // 実行中にアクティブな領域の色をラベルにも反映
                Color labelColor = (Application.isPlaying && _sequenceStartArea == i) ? region.detectedColor : Color.yellow;
                UnityEditor.Handles.Label(Gizmos.matrix.MultiplyPoint(center), $"R{i}", new GUIStyle { normal = { textColor = labelColor } });
            }
            #endif
        }

        // 行列を元に戻す
        Gizmos.matrix = oldGizmosMatrix;
    }
}