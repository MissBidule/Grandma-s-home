using UnityEngine;
using UnityEditor;
using PurrNet.Voice;

#if UNITY_EDITOR
namespace PurrVoice.Editor
{
    public class AudioDebugVisualizerWindow : EditorWindow
    {
        private PurrVoicePlayer _selectedPlayer;
        private Vector2 _scrollPosition;
        private const float WAVEFORM_HEIGHT = 80f;
        private const float PADDING = 10f;
        
        [MenuItem("Tools/PurrNet/Analysis/Audio Debug Visualizer")]
        public static void ShowWindow()
        {
            GetWindow<AudioDebugVisualizerWindow>("Audio Debug Visualizer");
        }
        
        private void OnEnable()
        {
            EditorApplication.update += Repaint;
        }
        
        private void OnDisable()
        {
            EditorApplication.update -= Repaint;
        }
        
        private void OnGUI()
        {
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Audio visualization is only available in Play Mode", MessageType.Info);
                return;
            }
            
            DrawPlayerSelector();
            
            if (_selectedPlayer == null)
            {
                EditorGUILayout.HelpBox("Select a PurrVoicePlayer to view audio debug information", MessageType.Info);
                return;
            }
            
            if (!_selectedPlayer.enableDebugVisualization)
            {
                EditorGUILayout.HelpBox("Debug visualization is disabled on the selected player. Enable this by attaching the PurrVoicePlayerDebug component on the prefab.", MessageType.Warning);
                return;
            }
            
            DrawPlayerInfo();
            DrawAudioVisualizers();
        }
        
        private void DrawPlayerSelector()
        {
            EditorGUILayout.LabelField("Player Selection", EditorStyles.boldLabel);
            
            var newPlayer = (PurrVoicePlayer)EditorGUILayout.ObjectField("Selected Player", _selectedPlayer, typeof(PurrVoicePlayer), true);
            
            if (newPlayer != _selectedPlayer)
            {
                _selectedPlayer = newPlayer;
            }
            
            var players = FindObjectsByType<PurrVoicePlayer>(FindObjectsInactive.Exclude, FindObjectsSortMode.InstanceID);
            if (players.Length > 0)
            {
                EditorGUILayout.LabelField("Quick Select:", EditorStyles.miniLabel);
                EditorGUILayout.BeginHorizontal();

                for (var i = 0; i < players.Length; i++)
                {
                    var player = players[i];
                    string buttonText = player.name;
                    if (player.isOwner) buttonText += " (Local)";
                    if (player.isServer) buttonText += " (Server)";

                    if (GUILayout.Button(buttonText, GUILayout.MaxWidth(150)))
                    {
                        _selectedPlayer = player;
                    }
                }

                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.Space();
        }
        
        private void DrawPlayerInfo()
        {
            EditorGUILayout.LabelField($"Player: {_selectedPlayer.name}", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Is Owner: {_selectedPlayer.isOwner}");
            EditorGUILayout.LabelField($"Is Server: {_selectedPlayer.isServer}");
            EditorGUILayout.LabelField($"Local Playback: {_selectedPlayer.usingLocalPlayback}");
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
        }
        
        private void DrawAudioVisualizers()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            
            if (_selectedPlayer.isOwner)
            {
                DrawVisualizerIfExists("Microphone Input", _selectedPlayer.micInputVisualizer, Color.green);
                DrawVisualizerIfExists("Sender Processed", _selectedPlayer.senderProcessedVisualizer, Color.blue);
                DrawVisualizerIfExists("Network Sent", _selectedPlayer.networkSentVisualizer, Color.cyan);
            }
            
            if (_selectedPlayer.isServer)
            {
                DrawVisualizerIfExists("Server Processed", _selectedPlayer.serverProcessedVisualizer, Color.yellow);
            }
            
            if (!_selectedPlayer.isOwner)
            {
                DrawVisualizerIfExists("Received", _selectedPlayer.receivedVisualizer, Color.magenta);
            }
            
            DrawVisualizerIfExists("Streamed Audio - Start", _selectedPlayer.streamedAudioVisualizerStart, Color.green);
            DrawVisualizerIfExists("Streamed Audio - End", _selectedPlayer.streamedAudioVisualizerEnd, Color.green);
            
            EditorGUILayout.EndScrollView();
        }
        
        private void DrawVisualizerIfExists(string label, AudioVisualizer visualizer, Color color)
        {
            if (visualizer == null) return;
            
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
            
            var samples = visualizer.GetSamples();
            if (samples.Length == 0)
            {
                EditorGUILayout.LabelField("No audio data", EditorStyles.centeredGreyMiniLabel);
                EditorGUILayout.Space();
                return;
            }
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Samples: {samples.Length}");
            EditorGUILayout.LabelField($"Max Amplitude: {visualizer.GetMaxAmplitude():F3}");
            EditorGUILayout.LabelField($"RMS: {visualizer.GetRMSAmplitude():F3}");
            EditorGUILayout.LabelField($"Time Window: {visualizer.GetCurrentTimeWindow():F2}s");
            EditorGUILayout.EndHorizontal();
            
            var rect = GUILayoutUtility.GetRect(0, WAVEFORM_HEIGHT, GUILayout.ExpandWidth(true));
            rect.x += PADDING;
            rect.width -= PADDING * 2;
            
            DrawWaveform(rect, samples, color);
            
            EditorGUILayout.Space();
        }
        
        private void DrawWaveform(Rect rect, float[] samples, Color color)
        {
            if (samples.Length < 2) return;
            
            EditorGUI.DrawRect(rect, new Color(0.1f, 0.1f, 0.1f, 0.8f));
            
            var centerY = rect.y + rect.height * 0.5f;
            Handles.color = new Color(0.3f, 0.3f, 0.3f, 0.8f);
            Handles.DrawLine(new Vector3(rect.x, centerY), new Vector3(rect.xMax, centerY));
            
            Handles.color = color;
            var points = new Vector3[samples.Length];
            
            float maxAmplitude = 0f;
            for (var i = 0; i < samples.Length; i++)
            {
                var sample = samples[i];
                maxAmplitude = Mathf.Max(maxAmplitude, Mathf.Abs(sample));
            }

            if (maxAmplitude == 0f) maxAmplitude = 1f; // Prevent division by zero
            
            for (int i = 0; i < samples.Length; i++)
            {
                float x = rect.x + (float)i / (samples.Length - 1) * rect.width;
                float normalizedSample = samples[i] / maxAmplitude;
                float y = centerY - normalizedSample * (rect.height * 0.4f);
                points[i] = new Vector3(x, y, 0);
            }
            
            for (int i = 0; i < points.Length - 1; i++)
            {
                Handles.DrawLine(points[i], points[i + 1]);
            }
            
            Handles.color = new Color(0.6f, 0.6f, 0.6f, 0.8f);
            var labelStyle = new GUIStyle(EditorStyles.miniLabel);
            labelStyle.normal.textColor = Color.gray;
            
            GUI.Label(new Rect(rect.x, rect.y, 50, 15), $"+{maxAmplitude:F2}", labelStyle);
            GUI.Label(new Rect(rect.x, rect.yMax - 15, 50, 15), $"-{maxAmplitude:F2}", labelStyle);
        }
    }
}
#endif