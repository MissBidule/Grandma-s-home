using UnityEngine;
using UnityEditor;

namespace Antony
{
    [CustomEditor(typeof(JellyGhostShaderManager))]
    public class JellyGhostShaderManagerHelper : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (GUILayout.Button("Update Renderers To Modify"))
            {
                ((JellyGhostShaderManager)(target)).UpdateRenderersToModify();
            }
        }
    }
}