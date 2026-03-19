using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PurrLobby
{
    /*
     * @brief Data container for an option row that displays a label and a clickable button.
     * Used as a prefab reference by settings panels to spawn action rows (e.g. "Apply", "Reset").
     */
    public class OptionRowButton : MonoBehaviour
    {
        [UnityEngine.Serialization.FormerlySerializedAs("label")]
        public TextMeshProUGUI m_label;
        [UnityEngine.Serialization.FormerlySerializedAs("button")]
        public Button m_button;
    }
}
