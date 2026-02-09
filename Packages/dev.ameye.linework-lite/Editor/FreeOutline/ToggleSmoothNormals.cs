using System;
using System.Collections.Generic;
using LineworkLite.FreeOutline;
using UnityEditor;
using UnityEngine;

namespace LineworkLite.Editor.FreeOutline
{
    public static class ToggleSmoothNormals
    {
        private const string MenuPath = "Assets/Calculate Smoothed Normals (Linework Lite)";

        [MenuItem(MenuPath, true)]
        public static bool ValidateToggle()
        {
            if (Selection.activeObject is not GameObject && Selection.activeObject is not Mesh) return false;

            var labels = AssetDatabase.GetLabels(Selection.activeObject);
            Menu.SetChecked(MenuPath, Array.Exists(labels, label => label == FreeOutlineUtils.SmoothNormalsLabel));
            return true;
        }

        [MenuItem(MenuPath)]
        public static void Toggle()
        {
            var selectedObject = Selection.activeObject;
            if (selectedObject == null) return;

            var labels = new List<string>(AssetDatabase.GetLabels(selectedObject));
            var previousLabels = new List<string>(labels);

            if (labels.Contains(FreeOutlineUtils.SmoothNormalsLabel))
            {
                labels.Remove(FreeOutlineUtils.SmoothNormalsLabel);
            }
            else
            {
                labels.Add(FreeOutlineUtils.SmoothNormalsLabel);
            }

            Undo.RecordObject(selectedObject, "Toggle Smooth Normals Label (Linework Lite)");

            AssetDatabase.SetLabels(selectedObject, labels.ToArray());

            Undo.undoRedoPerformed += () =>
            {
                AssetDatabase.SetLabels(selectedObject, previousLabels.ToArray());
            };
            
            var path = AssetDatabase.GetAssetPath(selectedObject);
            if (!string.IsNullOrEmpty(path))
            {
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }
        }
    }
}
