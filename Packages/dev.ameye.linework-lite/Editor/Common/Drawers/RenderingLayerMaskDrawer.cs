using System;
using System.Collections.Generic;
using LineworkLite.Common.Attributes;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace LineworkLite.Editor.Common.Drawers
{
    [CustomPropertyDrawer(typeof(RenderingLayerMaskAttribute))]
    public sealed class RenderingLayerMaskDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var attr = (RenderingLayerMaskAttribute) attribute;
            var renderingLayerMaskNames = new List<string>(GetRenderingLayerMaskNames());
            var maskField = new MaskField(attr.ShowLabel ? property.displayName : string.Empty, renderingLayerMaskNames, property.intValue);
            maskField.AddToClassList(MaskField.alignedFieldUssClassName);
            maskField.BindProperty(property);
            maskField.RegisterValueChangedCallback(x => SetValue(x.newValue, property));
            return maskField;
        }

        private static void SetValue(int intValue, SerializedProperty property)
        {
            property.uintValue = (uint) intValue;
            property.serializedObject.ApplyModifiedProperties();
        }

        private readonly GUIContent renderingLayerMaskContent = EditorGUIUtility.TrTextContent(
            "Rendering Layers",
            "Specify the rendering layer mask for this projector. Unity renders decals on all meshes where at least one Rendering Layer value matches."
        );

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var attr = (RenderingLayerMaskAttribute) attribute;
            var renderingLayer = (int) property.uintValue;
            var maskNames = GetRenderingLayerMaskNames();
            var maskCount = (int) Mathf.Log(renderingLayer, 2) + 1;

            if (maskNames.Length < maskCount && maskCount <= 32)
            {
                var newRenderingLayerMaskNames = new string[maskCount];

                for (var i = 0; i < maskCount; ++i)
                {
                    newRenderingLayerMaskNames[i] = i < maskNames.Length
                        ? maskNames[i]
                        : $"Unused Layer {i}";
                }

                maskNames = newRenderingLayerMaskNames;

                EditorGUILayout.HelpBox("One or more of the Rendering Layers is not defined in the Universal Global Settings asset.", MessageType.Warning);
            }

            EditorGUI.BeginProperty(position, renderingLayerMaskContent, property);

            EditorGUI.BeginChangeCheck();
            if (attr.ShowLabel)
            {
                renderingLayerMaskContent.text = property.displayName;
                renderingLayer = EditorGUI.MaskField(position, renderingLayerMaskContent, renderingLayer, maskNames);
            }
            else
            {
                renderingLayer = EditorGUI.MaskField(position, GUIContent.none, renderingLayer, maskNames);
            }

            if (EditorGUI.EndChangeCheck())
            {
                property.uintValue = (uint) renderingLayer;
            }

            EditorGUI.EndProperty();
        }

        private static string[] GetRenderingLayerMaskNames()
        {
#if UNITY_6000_0_OR_NEWER
            return RenderingLayerMask.GetDefinedRenderingLayerNames();
#else
            var renderPipeline = GraphicsSettings.currentRenderPipeline;
            return renderPipeline != null ? renderPipeline.renderingLayerMaskNames : Array.Empty<string>();
#endif
        }
    }
}
