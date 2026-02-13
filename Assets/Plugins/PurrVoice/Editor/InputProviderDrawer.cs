#if UNITY_EDITOR
using PurrNet.Voice;
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace PurrVoice.Editor
{
    [CustomPropertyDrawer(typeof(InputProvider), true)]
    public class InputProviderDrawer : PropertyDrawer
    {
        private Type[] _providerTypes;
        private string[] _typeNames;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            float buttonWidth = property.objectReferenceValue ? 0f : 50f;
            var fieldRect = new Rect(position.x, position.y, position.width - buttonWidth - 5, position.height);
            var buttonRect = new Rect(position.xMax - buttonWidth, position.y, buttonWidth, position.height);

            var oldColor = GUI.backgroundColor;
            if (!property.objectReferenceValue)
                GUI.backgroundColor = Color.yellow;

            EditorGUI.ObjectField(fieldRect, property, label);
            GUI.backgroundColor = oldColor;

            if (!property.objectReferenceValue && GUI.Button(buttonRect, "New"))
            {
                var target = (property.serializedObject.targetObject as Component)?.gameObject;
                if (target == null) return;

                _providerTypes ??= AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => a.GetTypes())
                    .Where(t => !t.IsAbstract && typeof(InputProvider).IsAssignableFrom(t))
                    .ToArray();

                _typeNames ??= _providerTypes.Select(t => t.Name).ToArray();

                var menu = new GenericMenu();
                for (int i = 0; i < _providerTypes.Length; i++)
                {
                    int index = i;
                    menu.AddItem(new GUIContent(_typeNames[i]), false, () =>
                    {
                        var comp = target.AddComponent(_providerTypes[index]);
                        property.objectReferenceValue = comp;
                        property.serializedObject.ApplyModifiedProperties();
                    });
                }

                menu.ShowAsContext();
            }

            EditorGUI.EndProperty();
        }
    }
}
#endif
