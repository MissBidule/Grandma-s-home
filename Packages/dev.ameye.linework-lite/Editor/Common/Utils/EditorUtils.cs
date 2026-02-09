using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LineworkLite.Editor.Common.Utils
{
    public static class EditorUtils
    {
        public static class CommonStyles
        {
            public static readonly GUIContent InjectionPoint = EditorGUIUtility.TrTextContent("Stage", "Controls when the render pass executes.");
            public static readonly GUIContent ShowInSceneView = EditorGUIUtility.TrTextContent("Show In Scene View", "Sets whether to render the pass in the scene view.");
            public static readonly GUIContent Scaling = EditorGUIUtility.TrTextContent("Scaling", "How to scale the width of the outline.");
            public static readonly GUIContent MinWidth = EditorGUIUtility.TrTextContent("Min Width", "The minimum width of the outline.");
            public static readonly GUIContent OutlineOccludedColor = EditorGUIUtility.TrTextContent("Occluded Color", "The color of the outline when it is occluded.");
            public static readonly GUIContent Outlines = EditorGUIUtility.TrTextContent("Outlines", "The list of outlines to render.");
            public static readonly GUIContent OutlineLayer = EditorGUIUtility.TrTextContent("Rendering Layer", "Only mesh renderers on this rendering layer will receive an outline.");
            public static readonly GUIContent LayerMask = EditorGUIUtility.TrTextContent("Layer Mask", "Only gameobjects on this layer will receive an outline.");
            public static readonly GUIContent RenderQueue = EditorGUIUtility.TrTextContent("Queue", "Only gameobjects using this render queue will receive an outline.");
            public static readonly GUIContent OutlineOcclusion = EditorGUIUtility.TrTextContent("Render", "For which occlusion states to render the outline.");
            public static readonly GUIContent OutlineBlendMode = EditorGUIUtility.TrTextContent("Blend", "How to blend the outline with the rest of the scene.");
            public static readonly GUIContent OutlineColor = EditorGUIUtility.TrTextContent("Color", "The color of the outline.");
            public static readonly GUIContent OutlineWidth = EditorGUIUtility.TrTextContent("Width", "The width of the outline.");
            public static readonly GUIContent ScaleWithResolution = EditorGUIUtility.TrTextContent("Scale With Resolution", "Scale the thickness of the outline with the resolution of the screen.");
            public static readonly GUIContent GpuInstancing = EditorGUIUtility.TrTextContent("GPU Instancing", "Use GPU instancing to render this outline layer.");
            public static readonly GUIContent MaskingStrategy = EditorGUIUtility.TrTextContent("Mask", "The masking strategy that is used to only show the outline where needed.");
            public static readonly GUIContent ExtrusionMethod = EditorGUIUtility.TrTextContent("Method", "The vertex extrusion method that is used.");
            public static readonly GUIContent MaterialType = EditorGUIUtility.TrTextContent("Type", "The alpha clip threshold.");
            public static readonly GUIContent CustomMaterial = EditorGUIUtility.TrTextContent("Material", "The alpha clip threshold.");
        }
        
        private static GUIStyle _buttonStyle;
        public static GUIStyle ButtonStyle => _buttonStyle ??= new GUIStyle(GUI.skin.button)
        {
            alignment = TextAnchor.MiddleLeft,
            stretchWidth = true,
            richText = true,
            wordWrap = true,
            padding = new RectOffset()
            {
                left = 7,
                right = 0,
                top = 5,
                bottom = 5
            },
            imagePosition = ImagePosition.ImageLeft
        };

        public static void OpenInspectorWindow(Object target)
        {
            var windowType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.InspectorWindow");
            EditorWindow.GetWindow(windowType);
            AssetDatabase.OpenAsset(target);
        }
    }
}
