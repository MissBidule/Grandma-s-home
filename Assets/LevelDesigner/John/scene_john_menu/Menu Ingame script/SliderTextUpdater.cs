using UnityEngine;
using UnityEngine.UI;
using TMPro; // Required for TextMeshPro

public class SliderTextUpdater : MonoBehaviour
{
    [Header("Drag your Text (TMP) here")]
    public TextMeshProUGUI percentageText;

    private Slider mySlider;

    void Start()
    {
        // Get the slider attached to this object
        mySlider = GetComponent<Slider>();

        // Update the text immediately when the game starts
        UpdateText(mySlider.value);

        // Tell the slider to trigger 'UpdateText' whenever it moves
        mySlider.onValueChanged.AddListener(UpdateText);
    }

    // This runs every time the slider moves
    public void UpdateText(float value)
    {
        // Converts the number to text and adds the "%" sign
        percentageText.text = value.ToString("0") + "%";
    }
}