using PurrNet.Voice;
using UnityEngine;
using UnityEditor;
using UnityEngine;

namespace PurrVoice.Editor
{
#if UNITY_EDITOR

    [CustomPropertyDrawer(typeof(PurrShowIfAttribute))]
    public class PurrShowIfDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var show = ShouldShow(property);
            return show ? EditorGUI.GetPropertyHeight(property, label, true) : 0;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (ShouldShow(property))
                EditorGUI.PropertyField(position, property, label, true);
        }

        private bool ShouldShow(SerializedProperty property)
        {
            var attr = (PurrShowIfAttribute)attribute;
            var target = property.serializedObject.FindProperty(attr.boolFieldName);
            return target != null && target.propertyType == SerializedPropertyType.Boolean && target.boolValue;
        }
    }
#endif

}
