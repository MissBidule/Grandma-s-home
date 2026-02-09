using LineworkLite.Editor.Common.Utils;
using LineworkLite.FreeOutline;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;

namespace LineworkLite.Editor.FreeOutline
{
    [CustomEditor(typeof(FreeOutlineSettings))]
    public class FreeOutlineSettingsEditor : UnityEditor.Editor
    {
        private SerializedProperty injectionPoint;
        private SerializedProperty showInSceneView;

        private SerializedProperty outlines;
        private EditorList<Outline> outlineList;

        private void OnEnable()
        {
            injectionPoint = serializedObject.FindProperty("injectionPoint");
            showInSceneView = serializedObject.FindProperty("showInSceneView");

            outlines = serializedObject.FindProperty("outlines");
            outlineList = new EditorList<Outline>(this, outlines, ForceSave, "Add Outline", "No outlines added.");
        }

        private void OnDisable()
        {
            outlineList.OnDisable();
        }

        public override void OnInspectorGUI()
        {
            if (outlines == null) OnEnable();

            serializedObject.Update();

            EditorGUILayout.LabelField("Free Outline", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(injectionPoint, EditorUtils.CommonStyles.InjectionPoint);
            EditorGUILayout.PropertyField(showInSceneView, EditorUtils.CommonStyles.ShowInSceneView);
            EditorGUILayout.Space();
            CoreEditorUtils.DrawSplitter();
            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.LabelField(EditorUtils.CommonStyles.Outlines, EditorStyles.boldLabel);
            outlineList.Draw();
            
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

        private void ForceSave()
        {
            ((FreeOutlineSettings) target).Changed();
            EditorUtility.SetDirty(target);
        }
    }
}
