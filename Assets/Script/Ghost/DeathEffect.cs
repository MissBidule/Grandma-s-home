using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class CameraEffect : MonoBehaviour
{
    private Volume volume;
    private ColorAdjustments colorAdjustments;
    [SerializeField] private float saturationValue = -30f;

    void Start()
    {
        volume = gameObject.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 10f;
        volume.profile = ScriptableObject.CreateInstance<VolumeProfile>();
        colorAdjustments = volume.profile.Add<ColorAdjustments>(true);
        colorAdjustments.saturation.overrideState = true;
        colorAdjustments.saturation.value = 0f;
    }

    /**
    @brief      Apply or remove the desaturation death effect on the camera
    @param      isDead  True to apply the effect, false to remove it
    */
    public void SetDeathEffect(bool isDead)
    {
        if(isDead)
        {
            colorAdjustments.saturation.value = 0f;
        }
        else
        {
            colorAdjustments.saturation.value = saturationValue;
        }
    }
}
