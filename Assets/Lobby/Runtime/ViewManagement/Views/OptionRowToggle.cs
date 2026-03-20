using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PurrLobby
{
    /*
     * @brief Data container for an option row that displays a label and an on/off toggle.
     * Used as a prefab reference by settings panels to spawn boolean rows (e.g. VSync, FPS counter).
     */
    public class OptionRowToggle : MonoBehaviour
    {
        [UnityEngine.Serialization.FormerlySerializedAs("label")]
        public TextMeshProUGUI m_label;
        [UnityEngine.Serialization.FormerlySerializedAs("toggle")]
        public Toggle          m_toggle;
    }
}
