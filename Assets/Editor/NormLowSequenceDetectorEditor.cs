using UnityEditor;
using UnityEngine;

// NormLowSequenceDetectorコンポーネントのカスタムエディタを定義
[CustomEditor(typeof(NormLowSequenceDetector))]
public class NormLowSequenceDetectorEditor : Editor
{
    // ランタイムスクリプトの参照
    private NormLowSequenceDetector detector;

    private void OnEnable()
    {
        // ターゲットコンポーネントの取得
        detector = (NormLowSequenceDetector)target;
    }

    // Sceneビューでの描画と操作を定義
    private void OnSceneGUI()
    {
        if (detector == null) return;

        float W = 640; 
        float H = 480; 

        Matrix4x4 gizmoMatrix = detector.transform.localToWorldMatrix * Matrix4x4.Translate(new Vector3(-W / 2f, -H / 2f, 0));
        
        EditorGUI.BeginChangeCheck();
        
        for (int i = 0; i < detector.regions.Length; i++)
        {
            SerializedProperty regionProp = serializedObject.FindProperty("regions").GetArrayElementAtIndex(i);
            
            NormLowSequenceDetector.DepthRegion region = detector.regions[i];
            
            Vector3 center = new Vector3(region.x + region.width / 2f, H - (region.y + region.height / 2f), 0);
            
            // ハンドルの色を検出色に合わせる（透明度を上げて見やすくする）
            Color handleColor = region.detectedColor;
            handleColor.a = 0.8f; // ハンドルは少し透過させて見やすく
            //Handles.color = (i == detector._sequenceStartArea && Application.isPlaying) ? Color.Lerp(handleColor, Color.white, Mathf.Sin(Time.time * 8f) * 0.5f + 0.5f) : handleColor;
            
            var fmh_44_17_638981126636393703 = Quaternion.identity; Vector3 newCenter = Handles.FreeMoveHandle(
                gizmoMatrix.MultiplyPoint(center),
                (region.width + region.height) / 4f * 0.1f, 
                Vector3.zero,
                Handles.SphereHandleCap
            );

            Vector3 localNewCenter = gizmoMatrix.inverse.MultiplyPoint(newCenter);

            int newX = Mathf.RoundToInt(localNewCenter.x - region.width / 2f);
            int newY = Mathf.RoundToInt(H - localNewCenter.y - region.height / 2f);
            
            Handles.Label(
                gizmoMatrix.MultiplyPoint(center + new Vector3(region.width / 2f + 5f, 0, 0)),
                $"{region.name}\nX:{region.x} Y:{region.y}\nW:{region.width} H:{region.height}",
                EditorStyles.boldLabel
            );
            
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(detector, $"Move Region {i}");

                detector.regions[i].x = newX;
                detector.regions[i].y = newY;
                
                EditorUtility.SetDirty(detector);
            }
        }
        
        DrawRegionGUI();
    }
    
    public override void OnInspectorGUI()
    {
        // デフォルトのインスペクター（publicフィールド）を描画
        DrawDefaultInspector(); 
        
        // 追加のGUIを描画
        DrawRegionGUI();
    }

    private void DrawRegionGUI()
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Region Quick Adjust & Color (Scene View Handle)", EditorStyles.boldLabel);
        
        EditorGUI.BeginChangeCheck();

        SerializedProperty regionsProp = serializedObject.FindProperty("regions");

        for (int i = 0; i < regionsProp.arraySize; i++)
        {
            SerializedProperty regionProp = regionsProp.GetArrayElementAtIndex(i);
            
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.LabelField(regionProp.FindPropertyRelative("name").stringValue, EditorStyles.miniBoldLabel);
            
            EditorGUILayout.BeginHorizontal();
            
            SerializedProperty xProp = regionProp.FindPropertyRelative("x");
            EditorGUILayout.PropertyField(xProp, new GUIContent("X", "領域の左上のX座標"), GUILayout.Width(100));

            SerializedProperty yProp = regionProp.FindPropertyRelative("y");
            EditorGUILayout.PropertyField(yProp, new GUIContent("Y", "領域の左上のY座標"), GUILayout.Width(100));

            SerializedProperty widthProp = regionProp.FindPropertyRelative("width");
            EditorGUILayout.PropertyField(widthProp, new GUIContent("W", "領域の幅"), GUILayout.Width(100));

            SerializedProperty heightProp = regionProp.FindPropertyRelative("height");
            EditorGUILayout.PropertyField(heightProp, new GUIContent("H", "領域の高さ"), GUILayout.Width(100));
            
            EditorGUILayout.EndHorizontal();

            // ★ColorFieldを追加★
            SerializedProperty detectedColorProp = regionProp.FindPropertyRelative("detectedColor");
            EditorGUILayout.PropertyField(detectedColorProp, new GUIContent("Detected Color", "検知時に表示される色"));

            EditorGUILayout.EndVertical();
        }

        if (EditorGUI.EndChangeCheck())
        {
            serializedObject.ApplyModifiedProperties();
        }
    }
}