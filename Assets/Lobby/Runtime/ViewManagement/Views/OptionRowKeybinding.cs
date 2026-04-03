using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PurrLobby
{
    /*
     * @brief Data container for an option row that displays a label and a rebindable key button.
     * Clicked by the player to start an interactive rebind; the button image changes colour
     * while waiting for input and the button label shows the current binding.
     */
    public class OptionRowKeybinding : MonoBehaviour
    {
        [UnityEngine.Serialization.FormerlySerializedAs("label")]
        public TextMeshProUGUI m_label;
        [UnityEngine.Serialization.FormerlySerializedAs("button")]
        public Button          m_button;
        [UnityEngine.Serialization.FormerlySerializedAs("buttonImage")]
        public Image           m_buttonImage;
        [UnityEngine.Serialization.FormerlySerializedAs("buttonLabel")]
        public TextMeshProUGUI m_buttonLabel;
    }
}
