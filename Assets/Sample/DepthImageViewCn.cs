using System.Collections;
using System.Collections.Generic;
using Orbbec;
using OrbbecUnity;
using UnityEngine;
using UnityEngine.UI;

public class DepthImageViewCn : MonoBehaviour
{
    public OrbbecFrameSource frameSource;

    private Texture2D depthTexture;
    private byte[] colorData;
    public float depthScale = 2700f; // ImageUtilsCn.csで使われている値

    // 💡 Inspectorで設定可能にするためのフィールド
    [Header("Depth Thresholds (m)")]
    public float minDepthThreshold = 0.5f; // MIN_DEPTH_THRESHOLD
    public float maxDepthThreshold = 1.28f; // MAX_DEPTH_THRESHOLD

    [Header("Color Band 1 (High Depth)")]
    [Range(0f, 1f)] public float normHighStart = 0.7f; // NormHighStart
    [Range(0f, 1f)] public float normHighEnd = 0.96f;   // NormHighEnd
    public Color highDepthColor = Color.blue; // 255 Blue

    [Header("Color Band 2 (Low Depth)")]
    [Range(0f, 1f)] public float normLowStart = 0.1f;  // NormLowStart
    [Range(0f, 1f)] public float normLowEnd = 0.4f;    // NormLowEnd
    public Color lowDepthColor = Color.red; // 255 Red

    [Header("Out of Range Color")]
    public Color outOfRangeColor = new Color32(50, 0, 0, 255); // 50 Red

    void Update()
    {
        var obDepthFrame = frameSource.GetDepthFrame();

        if (obDepthFrame == null || obDepthFrame.width == 0 || obDepthFrame.height == 0 || obDepthFrame.data == null || obDepthFrame.data.Length == 0)
        {
            return;
        }
        if (obDepthFrame.frameType != FrameType.OB_FRAME_DEPTH)
        {
            return;
        }
        if (depthTexture == null)
        {
            depthTexture = new Texture2D(obDepthFrame.width, obDepthFrame.height, TextureFormat.RGB24, false);
            Debug.Log($"NowScale:"+ obDepthFrame.width + "," + obDepthFrame.height);
            GetComponent<Renderer>().material.mainTexture = depthTexture;
        }
        if (depthTexture.width != obDepthFrame.width || depthTexture.height != obDepthFrame.height)
        {
            depthTexture.Reinitialize(obDepthFrame.width, obDepthFrame.height);
        }

        int colorDataLength = (obDepthFrame.data.Length / 2) * 3;
        if (colorData == null || colorData.Length != colorDataLength)
        {
            colorData = new byte[colorDataLength];
        }

        // ImageUtilsCn.ConvertDepthToColorDataの代わりにローカルメソッドを呼び出す
        ConvertDepthToColorData(obDepthFrame.data, ref colorData);

        depthTexture.LoadRawTextureData(colorData);
        depthTexture.Apply();
    }

    /// <summary>
    /// 深度データ(16bit)をRGBカラーデータ(24bit)に変換する。
    /// </summary>
    private void ConvertDepthToColorData(byte[] depthData, ref byte[] colorData)
    {
        float DEPTH_RANGE = maxDepthThreshold - minDepthThreshold;

        // Color型の値を0-255のbyte値に変換
        byte oR = (byte)(outOfRangeColor.r * 255);
        byte oG = (byte)(outOfRangeColor.g * 255);
        byte oB = (byte)(outOfRangeColor.b * 255);

        byte hR = (byte)(highDepthColor.r * 255);
        byte hG = (byte)(highDepthColor.g * 255);
        byte hB = (byte)(highDepthColor.b * 255);

        byte lR = (byte)(lowDepthColor.r * 255);
        byte lG = (byte)(lowDepthColor.g * 255);
        byte lB = (byte)(lowDepthColor.b * 255);


        for (int i = 0; i < depthData.Length; i += 2)
        {
            // 1. 16ビット深度値の取得
            ushort depthValue = (ushort)(depthData[i + 1] << 8 | depthData[i]);
            // 2. 実際の深度計算
            float depth = (float)depthValue / depthScale;
            int index = (i / 2) * 3;

            // 3. 深度が有効範囲外かどうか
            if (depthValue == 0 || depth < minDepthThreshold || depth > maxDepthThreshold)
            {
                colorData[index] = oR;
                colorData[index + 1] = oG;
                colorData[index + 2] = oB;
            }
            else
            {
                // 4. 深度を正規化 (0.0 ～ 1.0)
                float normalizedDepth = (depth - minDepthThreshold) / DEPTH_RANGE;

                if (normalizedDepth >= normHighStart && normalizedDepth <= normHighEnd)
                {
                    // 高深度範囲の色
                    colorData[index] = hR;
                    colorData[index + 1] = hG;
                    colorData[index + 2] = hB;
                }
                else if (normalizedDepth >= normLowStart && normalizedDepth <= normLowEnd)
                {
                    // 低深度範囲の色
                    colorData[index] = lR;
                    colorData[index + 1] = lG;
                    colorData[index + 2] = lB;
                }
                else
                {
                    // どの特定範囲にも属さない場合
                    // 元のコードではここに処理がなかったので、ここでは黒(0,0,0)にしておきます。
                    // 必要に応じてグレースケールや別の色を設定してください。
                    colorData[index] = 0;
                    colorData[index + 1] = 0;
                    colorData[index + 2] = 0;
                }
            }
        }
    }
}