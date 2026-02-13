using System;
using UnityEngine;

namespace PurrNet.Voice
{
    public class AudioDebugWaveform : MonoBehaviour
    {
        private static AudioDebugWaveform _instance;

        [SerializeField] private Rect _drawArea = new Rect(10, 10, 600, 150);
        [SerializeField] private int frequency = 48000;
        [SerializeField] private float timeWindow = 3f;
        [SerializeField] private int targetSamples = 300;

        private AudioVisualizer _visualizer;

        private void Awake()
        {
            _instance = this;
            _visualizer = new AudioVisualizer(timeWindow, frequency, targetSamples);
        }

        public static void SetSamples(ArraySegment<float> samples)
        {
            _instance?._visualizer.AddSamples(samples);
        }

        private void OnGUI()
        {
            var data = _visualizer.GetSamples();
            if (data.Length < 2) return;

            Vector2 prev = Vector2.zero;
            for (int i = 0; i < data.Length; i++)
            {
                float x = _drawArea.x + ((float)i / (data.Length - 1)) * _drawArea.width;
                float y = _drawArea.y + (_drawArea.height / 2f) - (data[i] * _drawArea.height / 2f);
                Vector2 current = new Vector2(x, y);

                if (i > 0)
                    Drawing.DrawLine(prev, current, Color.green, 2f);

                prev = current;
            }
        }
    }

    public static class Drawing
    {
        private static Texture2D _lineTex;

        public static void DrawLine(Vector2 pointA, Vector2 pointB, Color color, float width)
        {
            if (_lineTex == null)
            {
                _lineTex = new Texture2D(1, 1);
                _lineTex.SetPixel(0, 0, Color.white);
                _lineTex.Apply();
            }

            Matrix4x4 matrix = GUI.matrix;

            Color savedColor = GUI.color;
            GUI.color = color;

            float angle = Vector3.Angle(pointB - pointA, Vector2.right);
            if (pointA.y > pointB.y) angle = -angle;

            float length = (pointB - pointA).magnitude;
            GUIUtility.RotateAroundPivot(angle, pointA);
            GUI.DrawTexture(new Rect(pointA.x, pointA.y - (width / 2), length, width), _lineTex);

            GUI.matrix = matrix;
            GUI.color = savedColor;
        }
    }
}
