using UnityEngine;
using UnityEditor;

namespace Antony
{
    [CustomEditor(typeof(PlaydoughShaderManager))]
    public class PlaydoughShaderManagerHelper : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (GUILayout.Button("Update Renderers To Modify"))
            {
                ((PlaydoughShaderManager)(target)).UpdateRenderersToModify();
            }
        }
    }
}