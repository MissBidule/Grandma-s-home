using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;

namespace LineworkLite.Editor.Common.Utils
{
    public class EditorList<T> where T : ScriptableObject
    {
        private SerializedProperty Items { get; }
        private readonly List<UnityEditor.Editor> editors;
        private readonly UnityEditor.Editor targetEditor;
        private readonly Action onChangedCallback;
        private readonly string addItemText, noItemsText, maxItemsText;
        private readonly int maxItems;

        public EditorList(UnityEditor.Editor targetEditor, SerializedProperty items, Action onChangedCallback, string addItemText, string noItemsText, string maxItemsText = "",
            int maxItems = 0)
        {
            Items = items;
            this.targetEditor = targetEditor;
            this.onChangedCallback = onChangedCallback;
            this.addItemText = addItemText;
            this.noItemsText = noItemsText;
            this.maxItems = maxItems;
            this.maxItemsText = maxItemsText;
            editors = new List<UnityEditor.Editor>();

            UpdateEditors();
        }

        public void OnDisable()
        {
            ClearEditors();
        }

        public void Draw()
        {
            if (Items.arraySize == 0)
            {
                EditorGUILayout.HelpBox(noItemsText, MessageType.Info);
            }
            else
            {
                CoreEditorUtils.DrawSplitter();
                for (var i = 0; i < Items.arraySize; i++)
                {
                    var item = Items.GetArrayElementAtIndex(i);
                    DrawItem(i, ref item);
                    CoreEditorUtils.DrawSplitter();
                }
            }
            EditorGUILayout.Space();

            var reachedMaxItems = maxItems > 0 && Items.arraySize >= maxItems;

            if (reachedMaxItems)
            {
                EditorGUILayout.HelpBox(maxItemsText, MessageType.Info);
                EditorGUILayout.Space();
            }

            using (new EditorGUI.DisabledScope(reachedMaxItems))
            {
                if (GUILayout.Button(addItemText, EditorStyles.miniButton)) AddItem();
            }
        }

        private void DrawItem(int index, ref SerializedProperty itemProperty)
        {
            var item = itemProperty.objectReferenceValue;
            if (item != null)
            {
                var hasChangedProperties = false;
                var itemEditor = editors[index];
                var serializedObjectEditor = itemEditor.serializedObject;
                serializedObjectEditor.Update();

                EditorGUI.BeginChangeCheck();
                var activeProperty = serializedObjectEditor.FindProperty("isActive");
                var displayContent = CoreEditorUtils.DrawHeaderToggle(
                    EditorGUIUtility.TrTextContent(ObjectNames.GetInspectorTitle(item).Replace($" ({typeof(T).Name})", ""), $"An outline."), itemProperty, activeProperty,
                    pos => OnContextClick(item, pos, index));
                hasChangedProperties |= EditorGUI.EndChangeCheck();

                if (displayContent)
                {
                    EditorGUI.BeginChangeCheck();
                    itemEditor.OnInspectorGUI();
                    hasChangedProperties |= EditorGUI.EndChangeCheck();
                }

                if (hasChangedProperties)
                {
                    serializedObjectEditor.ApplyModifiedProperties();
                    targetEditor.serializedObject.ApplyModifiedProperties();
                    onChangedCallback?.Invoke();
                }
            }
            else
            {
                EditorGUILayout.LabelField("NULL ITEM");
            }
        }

        private void OnContextClick(UnityEngine.Object rendererFeatureObject, Vector2 position, int id)
        {
            var menu = new GenericMenu();

            if (id == 0)
                menu.AddDisabledItem(EditorGUIUtility.TrTextContent("Move Up"));
            else
                menu.AddItem(EditorGUIUtility.TrTextContent("Move Up"), false, () => MoveItem(id, -1));

            if (id == Items.arraySize - 1)
                menu.AddDisabledItem(EditorGUIUtility.TrTextContent("Move Down"));
            else
                menu.AddItem(EditorGUIUtility.TrTextContent("Move Down"), false, () => MoveItem(id, 1));

            menu.AddSeparator(string.Empty);
            menu.AddItem(EditorGUIUtility.TrTextContent("Remove"), false, () => RemoveItem(id));

            menu.DropDown(new Rect(position, Vector2.zero));
        }

        private void AddItem()
        {
            targetEditor.serializedObject.Update();

            var component = ScriptableObject.CreateInstance<T>();
            component.name = $"{typeof(T).Name}";
            Undo.RegisterCreatedObjectUndo(component, "Add Item");

            if (EditorUtility.IsPersistent(targetEditor.target))
            {
                AssetDatabase.AddObjectToAsset(component, targetEditor.target);
            }
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(component, out _, out long _);

            Items.arraySize++;
            var componentProp = Items.GetArrayElementAtIndex(Items.arraySize - 1);
            componentProp.objectReferenceValue = component;

            UpdateEditors();
            targetEditor.serializedObject.ApplyModifiedProperties();

            if (EditorUtility.IsPersistent(targetEditor.target))
            {
                onChangedCallback?.Invoke();
            }
            targetEditor.serializedObject.ApplyModifiedProperties();
        }

        private void RemoveItem(int index)
        {
            var property = Items.GetArrayElementAtIndex(index);
            var item = property.objectReferenceValue;
            property.objectReferenceValue = null;
            Undo.SetCurrentGroupName(item == null ? "Remove Item" : $"Remove {item.name}");

            Items.DeleteArrayElementAtIndex(index);
            UpdateEditors();
            targetEditor.serializedObject.ApplyModifiedProperties();

            if (item != null)
            {
                Undo.DestroyObjectImmediate(item);
            }

            onChangedCallback?.Invoke();
        }

        private void MoveItem(int index, int offset)
        {
            Undo.SetCurrentGroupName("Move Item");
            targetEditor.serializedObject.Update();
            Items.MoveArrayElement(index, index + offset);
            UpdateEditors();
            targetEditor.serializedObject.ApplyModifiedProperties();
            onChangedCallback?.Invoke();
        }

        private void UpdateEditors()
        {
            ClearEditors();

            for (var i = 0; i < Items.arraySize; i++)
            {
                editors.Add(UnityEditor.Editor.CreateEditor(Items.GetArrayElementAtIndex(i).objectReferenceValue));
            }
        }

        private void ClearEditors()
        {
            for (var i = editors.Count - 1; i >= 0; --i)
            {
                UnityEngine.Object.DestroyImmediate(editors[i]);
            }
            editors.Clear();
        }
    }
}
