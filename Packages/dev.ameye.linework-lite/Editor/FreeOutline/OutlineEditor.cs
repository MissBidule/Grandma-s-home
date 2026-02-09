using System;
using LineworkLite.Common.Utils;
using LineworkLite.Editor.Common.Utils;
using UnityEditor;
using UnityEngine;
using Outline = LineworkLite.FreeOutline.Outline;
using Resolution = LineworkLite.Common.Utils.Resolution;
using Scaling = LineworkLite.FreeOutline.Scaling;

namespace LineworkLite.Editor.FreeOutline
{
    [CustomEditor(typeof(Outline))]
    public class OutlineEditor : UnityEditor.Editor
    {
        private SerializedProperty renderingLayer;
        private SerializedProperty layerMask;
        private SerializedProperty renderQueue;
        private SerializedProperty occlusion;
        private SerializedProperty maskingStrategy;
        private SerializedProperty blendMode;
        private SerializedProperty color;
        private SerializedProperty enableOcclusion;
        private SerializedProperty occludedColor;
        private SerializedProperty extrusionMethod;
        private SerializedProperty scaling;
        private SerializedProperty width;
        private SerializedProperty minWidth;
        private SerializedProperty scaleWithResolution;
        private SerializedProperty referenceResolution;
        private SerializedProperty customReferenceResolution;
        private SerializedProperty materialType;
        private SerializedProperty customMaterial;

        private void OnEnable()
        {
            renderingLayer = serializedObject.FindProperty(nameof(Outline.RenderingLayer));
            layerMask = serializedObject.FindProperty(nameof(Outline.layerMask));
            renderQueue = serializedObject.FindProperty(nameof(Outline.renderQueue));
            occlusion = serializedObject.FindProperty(nameof(Outline.occlusion));
            maskingStrategy = serializedObject.FindProperty(nameof(Outline.maskingStrategy));
            blendMode = serializedObject.FindProperty(nameof(Outline.blendMode));
            color = serializedObject.FindProperty(nameof(Outline.color));
            enableOcclusion = serializedObject.FindProperty(nameof(Outline.enableOcclusion));
            occludedColor = serializedObject.FindProperty(nameof(Outline.occludedColor));
            extrusionMethod = serializedObject.FindProperty(nameof(Outline.extrusionMethod));
            scaling = serializedObject.FindProperty(nameof(Outline.scaling));
            width = serializedObject.FindProperty(nameof(Outline.width));
            minWidth = serializedObject.FindProperty(nameof(Outline.minWidth));
            scaleWithResolution = serializedObject.FindProperty(nameof(Outline.scaleWithResolution));
            referenceResolution = serializedObject.FindProperty(nameof(Outline.referenceResolution));
            customReferenceResolution = serializedObject.FindProperty(nameof(Outline.customResolution));
            materialType = serializedObject.FindProperty(nameof(Outline.materialType));
            customMaterial = serializedObject.FindProperty(nameof(Outline.customMaterial));
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Filters", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(renderingLayer, EditorUtils.CommonStyles.OutlineLayer);
            EditorGUILayout.PropertyField(layerMask, EditorUtils.CommonStyles.LayerMask);
            EditorGUILayout.PropertyField(renderQueue, EditorUtils.CommonStyles.RenderQueue);
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Render", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(occlusion, EditorUtils.CommonStyles.OutlineOcclusion);
            EditorGUILayout.PropertyField(blendMode, EditorUtils.CommonStyles.OutlineBlendMode);
            if ((Occlusion) occlusion.intValue == Occlusion.WhenNotOccluded)
            {
                EditorGUILayout.PropertyField(maskingStrategy, EditorUtils.CommonStyles.MaskingStrategy);
            }
           
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Outline", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(materialType, EditorUtils.CommonStyles.MaterialType);
            switch ((MaterialType) materialType.intValue)
            {
                case MaterialType.Basic:
                    EditorGUILayout.PropertyField(color, EditorUtils.CommonStyles.OutlineColor);
                    if ((Occlusion) occlusion.intValue == Occlusion.Always)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.PropertyField(enableOcclusion, EditorUtils.CommonStyles.OutlineOccludedColor);
                        if (enableOcclusion.boolValue) EditorGUILayout.PropertyField(occludedColor, GUIContent.none);
                        EditorGUILayout.EndHorizontal();
                    }
                    EditorGUILayout.PropertyField(extrusionMethod, EditorUtils.CommonStyles.ExtrusionMethod);
                    EditorGUILayout.PropertyField(scaling, EditorUtils.CommonStyles.Scaling);
                    switch ((Scaling) scaling.intValue)
                    {
                        case Scaling.ConstantScreenSize:
                            EditorGUILayout.PropertyField(width, EditorUtils.CommonStyles.OutlineWidth);
                            break;
                        case Scaling.ScaleWithDistance:
                            EditorGUILayout.PropertyField(width, EditorUtils.CommonStyles.OutlineWidth);
                            EditorGUILayout.PropertyField(minWidth, EditorUtils.CommonStyles.MinWidth);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.PropertyField(scaleWithResolution, EditorUtils.CommonStyles.ScaleWithResolution);
                    if (scaleWithResolution.boolValue)
                    {
                        EditorGUI.indentLevel++;
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.PropertyField(referenceResolution, GUIContent.none);
                        if ((Resolution) referenceResolution.intValue == Resolution.Custom) EditorGUILayout.PropertyField(customReferenceResolution, GUIContent.none, GUILayout.Width(100));
                        EditorGUILayout.EndHorizontal();
                        EditorGUI.indentLevel--;
                    }
                    EditorGUILayout.EndHorizontal();
                    break;
                case MaterialType.Custom:
                    EditorGUILayout.PropertyField(customMaterial, EditorUtils.CommonStyles.CustomMaterial);
                    break;
            }
            EditorGUILayout.Space();

            serializedObject.ApplyModifiedProperties();
        }
    }
}
