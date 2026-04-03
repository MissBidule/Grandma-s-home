using TMPro;
using UnityEngine;

namespace PurrLobby
{
    /*
     * @brief Lightweight FPS counter overlay rendered on top of all UI.
     * Builds its own Canvas (ScreenSpaceOverlay, sortingOrder 999) at runtime so it does not
     * depend on any scene hierarchy. Updates every 0.5 s and colour-codes the text:
     * green ≥ 60 fps, yellow ≥ 30 fps, red below 30 fps.
     */
    public class FpsCounter : MonoBehaviour
    {
        private TextMeshProUGUI m_text;
        private float m_timer;
        private int m_frames;

        /*
         * @brief Creates the Canvas and the TextMeshPro label used by this counter.
         */
        private void Awake()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999;
            gameObject.AddComponent<UnityEngine.UI.CanvasScaler>();

            var go = new GameObject("FPS", typeof(RectTransform));
            go.transform.SetParent(transform, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-10f, -10f);
            rt.sizeDelta = new Vector2(120f, 30f);

            m_text = go.AddComponent<TextMeshProUGUI>();
            m_text.fontSize = 18;
            m_text.alignment = TextAlignmentOptions.TopRight;
            m_text.color = Color.white;
            m_text.outlineWidth = 0.2f;
            m_text.outlineColor = Color.black;
        }

        /*
         * @brief Accumulates frames and refreshes the display every 0.5 s.
         */
        private void Update()
        {
            m_frames++;
            m_timer += Time.unscaledDeltaTime;

            if (m_timer < 0.5f)
            {
                return;
            }

            int fps = Mathf.RoundToInt(m_frames / m_timer);
            m_text.text = $"{fps} FPS";
            m_text.color = fps >= 60 ? Color.green
                        : fps >= 30 ? Color.yellow
                                    : Color.red;
            m_frames = 0;
            m_timer  = 0f;
        }
    }
}
