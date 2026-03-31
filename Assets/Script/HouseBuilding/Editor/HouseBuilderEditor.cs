#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Script.HouseBuilding
{
    [CustomEditor(typeof(HouseBuilder))]
    public class HouseBuilderEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            GUILayout.Space(10);

            HouseBuilder builder = (HouseBuilder)target;

            GUI.backgroundColor = Color.green;

            if (GUILayout.Button("Build House (Editor)"))
            {
                builder.EditorBuildHouse();
            }
            
            GUI.backgroundColor = Color.purple;
            
            if (GUILayout.Button("Randomize Editor Seed"))
            {
                builder.RandomizeSeed();
            }

            GUI.backgroundColor = Color.red;

            if (GUILayout.Button("Clear Spawned House"))
            {
                builder.ClearEditorHouse();
            }

            GUI.backgroundColor = Color.white;
        }
    }
}
#endif