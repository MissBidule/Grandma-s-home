using TMPro;
using UnityEngine;

namespace PurrLobby
{
    /*
     * @brief Data container for a decorative section title row inside a settings panel.
     * Spawned by settings panels to visually separate groups of option rows (e.g. "Display", "Quality").
     */
    public class OptionSectionTitle : MonoBehaviour
    {
        [UnityEngine.Serialization.FormerlySerializedAs("title")]
        public TextMeshProUGUI m_title;
    }
}
