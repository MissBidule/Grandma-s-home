using TMPro;
using UnityEngine;

namespace PurrLobby
{
    /*
     * @brief Data container for an option row that displays a label and a dropdown selector.
     * Used as a prefab reference by settings panels to spawn choice rows (e.g. resolution, display mode).
     */
    public class OptionRowDropdown : MonoBehaviour
    {
        [UnityEngine.Serialization.FormerlySerializedAs("label")]
        public TextMeshProUGUI m_label;
        [UnityEngine.Serialization.FormerlySerializedAs("dropdown")]
        public TMP_Dropdown m_dropdown;
    }
}
