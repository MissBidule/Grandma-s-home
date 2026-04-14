#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace Script.Audio.Editor
{
    [CustomEditor(typeof(MusicLooper))]
    public class MusicLooperEditor : UnityEditor.Editor
    {
        private float m_musicVolume = 1f;
        
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            
            GUILayout.Space(10);
            
            MusicLooper looper = (MusicLooper)target;
            
            GUI.backgroundColor = Color.blue;

            if (GUILayout.Button("Play Game Music"))
            {
                looper.PlayMusic(MusicTrack.Game);
            }
            
            GUI.backgroundColor = Color.darkRed;
            
            if (GUILayout.Button("Play Panic Music"))
            {
                looper.PlayMusic(MusicTrack.Panic);
            }
            
            GUI.backgroundColor = Color.yellow;
            
            if (GUILayout.Button("Reset Audio Source (if you clicked the button outside of play mode)"))
            {
                looper.Reset();
            }
            
            GUI.backgroundColor = Color.white;
            
            GUILayout.Space(10);
            GUILayout.Label("Music Volume");

            EditorGUI.BeginChangeCheck();
            m_musicVolume = GUILayout.HorizontalSlider(m_musicVolume, 0f, 1f);
            if (EditorGUI.EndChangeCheck())
            {
                looper.SetMusicVolume(m_musicVolume);
            }
            GUILayout.Space(10);
        }
    }
}
#endif