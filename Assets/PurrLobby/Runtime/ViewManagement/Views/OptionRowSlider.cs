using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PurrLobby
{
    /*
     * @brief Data container for an option row that displays a label, a slider, and a current-value label.
     * Used as a prefab reference by settings panels to spawn numeric-range rows (e.g. volume, sensitivity).
     */
    public class OptionRowSlider : MonoBehaviour
    {
        [UnityEngine.Serialization.FormerlySerializedAs("label")]
        public TextMeshProUGUI m_label;
        [UnityEngine.Serialization.FormerlySerializedAs("slider")]
        public Slider          m_slider;
        [UnityEngine.Serialization.FormerlySerializedAs("valueLabel")]
        public TextMeshProUGUI m_valueLabel;
    }
}
