using System.Linq;
using LineworkLite.Editor.Common.Utils;
using LineworkLite.FreeOutline;
using UnityEditor;
using UnityEngine;

namespace LineworkLite.Editor.FreeOutline
{
    [CustomEditor(typeof(LineworkLite.FreeOutline.FreeOutline))]
    public class FreeOutlineEditor : UnityEditor.Editor
    {
        private static class Styles
        {
            public static readonly GUIContent Settings = EditorGUIUtility.TrTextContent("Settings", "The settings for the Free Outline renderer feature.");
        }

        private SerializedProperty settings;

        private bool initialized;

        private void Initialize()
        {
            settings = serializedObject.FindProperty("settings");
            initialized = true;
        }

        public override void OnInspectorGUI()
        {
            if (!initialized) Initialize();
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(settings, Styles.Settings);

            if (settings.objectReferenceValue == null)
            {
                if (GUILayout.Button("Create", EditorStyles.miniButton, GUILayout.Width(70.0f)))
                {
                    const string path = "Assets/Free Outline Settings.asset";

                    var createdSettings = CreateInstance<FreeOutlineSettings>();
                    AssetDatabase.CreateAsset(createdSettings, path);
                    AssetDatabase.SaveAssets();
                    EditorUtility.FocusProjectWindow();
                    Selection.activeObject = createdSettings;
                    EditorUtils.OpenInspectorWindow(createdSettings);
                    settings.objectReferenceValue = createdSettings;
                    serializedObject.ApplyModifiedProperties();
                }
            }
            else
            {
                if (GUILayout.Button("Open", EditorStyles.miniButton, GUILayout.Width(70.0f)))
                {
                    EditorUtils.OpenInspectorWindow(settings.objectReferenceValue);
                }
            }
            EditorGUILayout.EndHorizontal();

            if (settings.objectReferenceValue != null && !((FreeOutlineSettings) settings.objectReferenceValue).Outlines.Any(outline => outline.IsActive()))
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox("No active outlines present. Effect will not render. Open the settings to add/enable outlines.", MessageType.Warning);
            }
            
            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                var content = EditorGUIUtility.IconContent("d_AssetStore Icon");
                content.text = "<b><size=12> Upgrade Linework</size></b>\n Includes smoother outlines, edge detection and fill effects.";
                content.tooltip = "Click to open asset page";
                
                if (GUILayout.Button(content, EditorUtils.ButtonStyle, GUILayout.Height(50f)))
                {
                    Application.OpenURL("https://assetstore.unity.com/packages/vfx/shaders/linework-outlines-and-edge-detection-294140");
                }
            }
        }
    }
}
