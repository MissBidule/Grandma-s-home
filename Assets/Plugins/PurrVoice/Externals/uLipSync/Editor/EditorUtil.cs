using UnityEngine;
using UnityEditor;

namespace uLipSync
{

public static class EditorUtil
{
    public static float lineHeightWithMargin =>
        EditorGUIUtility.singleLineHeight +
        EditorGUIUtility.standardVerticalSpacing;

    private static string GetKey(string title, string category)
    {
        return $"{Common.AssetName}-{category}-{title}";
    }

    private static string GetFoldOutKey(string title)
    {
        return GetKey(title, "FoldOut");
    }

    public static bool IsFoldOutOpened(string title, bool initialState = false, string additionalKey = "")
    {
        var key = GetFoldOutKey(title + additionalKey);
        if (!EditorPrefs.HasKey(key)) return initialState;
        return EditorPrefs.GetBool(key);
    }

    public static bool Foldout(string title, bool initialState, string additionalKey = "")
    {
        var style = new GUIStyle("ShurikenModuleTitle")
        {
            font = new GUIStyle(EditorStyles.label).font,
            border = new RectOffset(15, 7, 4, 4),
            fixedHeight = 22,
            contentOffset = new Vector2(20f, -2f),
            margin = new RectOffset((EditorGUI.indentLevel + 1) * 16, 0, 0, 0)
        };

        var key = GetFoldOutKey(title + additionalKey);
        bool display = EditorPrefs.GetBool(key, initialState);

        var rect = GUILayoutUtility.GetRect(16f, 22f, style);
        GUI.Box(rect, title, style);

        var e = Event.current;

        var toggleRect = new Rect(rect.x + 4f, rect.y + 2f, 13f, 13f);
        if (e.type == EventType.Repaint)
        {
            EditorStyles.foldout.Draw(toggleRect, false, false, display, false);
        }

        if (e.type == EventType.MouseDown && rect.Contains(e.mousePosition))
        {
            EditorPrefs.SetBool(key, !display);
            e.Use();
        }

        return display;
    }

    public static bool SimpleFoldout(Rect rect, string title, bool initialState, string additionalKey = "")
    {
        var key = GetFoldOutKey(title + additionalKey);
        bool display = EditorPrefs.GetBool(key, initialState);
        bool newDisplay = EditorGUI.Foldout(rect, display, title);
        if (newDisplay != display) EditorPrefs.SetBool(key, newDisplay);
        return newDisplay;
    }

    public static bool SimpleFoldout(string title, bool initialState, string additionalKey = "")
    {
        var key = GetFoldOutKey(title + additionalKey);
        bool display = EditorPrefs.GetBool(key, initialState);
        bool newDisplay = EditorGUILayout.Foldout(display, title, EditorStyles.foldoutHeader);
        if (newDisplay != display) EditorPrefs.SetBool(key, newDisplay);
        return newDisplay;
    }

    public static void DrawProperty(SerializedObject obj, string propName)
    {
        var prop = obj.FindProperty(propName);
        if (prop == null) return;
        EditorGUILayout.PropertyField(prop);
    }

    public static void DrawBackgroundRect(Rect rect, Color bg, Color line)
    {
        Handles.DrawSolidRectangleWithOutline(rect, bg, line);
    }

    public static void DrawBackgroundRect(Rect rect)
    {
        DrawBackgroundRect(
            rect,
            new Color(0f, 0f, 0f, 0.2f),
            new Color(1f, 1f, 1f, 0.2f));
    }

    public class DrawWaveOption
    {
        public System.Func<float, Color> colorFunc = _ => new Color(1f, 0.5f, 0f, 1f);
        public float waveScale = 0.95f;
    }

    public static void DrawWave(Rect rect, AudioClip clip, DrawWaveOption option)
    {
        if (!clip) return;

        var minMaxData = AudioUtil.GetMinMaxData(clip);
        if (minMaxData == null) return;

        int channels = clip.channels;
        int samples = minMaxData.Length / (2 * channels);

        AudioCurveRendering.AudioMinMaxCurveAndColorEvaluator dlg = delegate(
            float x,
            out Color col,
            out float minValue,
            out float maxValue)
        {
            col = option.colorFunc(x);

            float p = Mathf.Clamp(x * (samples - 2), 0f, samples - 2);
            int i = (int)Mathf.Floor(p);
            int offset1 = (i * channels) * 2;
            int offset2 = offset1 + channels * 2;
            minValue = Mathf.Min(minMaxData[offset1 + 1], minMaxData[offset2 + 1]) * option.waveScale;
            maxValue = Mathf.Max(minMaxData[offset1 + 0], minMaxData[offset2 + 0]) * option.waveScale;

            if (minValue > maxValue)
            {
                (minValue, maxValue) = (maxValue, minValue);
            }
        };

        AudioCurveRendering.DrawMinMaxFilledCurve(rect, dlg);
    }

    public static T CreateAssetInRoot<T>(string name) where T : ScriptableObject
    {
        var path = AssetDatabase.GenerateUniqueAssetPath($"Assets/{name}.asset");
        var obj = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(obj, path);
        return AssetDatabase.LoadAssetAtPath<T>(path);
    }
}

}
