using UnityEngine;

public class LightData : MonoBehaviour
{
    [HideInInspector] public float lightIntensity;//intensité cible pour la lumière
    [HideInInspector] public Light pointLight;

    void Awake() {
        pointLight = GetComponent<Light>();//récupère le component Light en tant que référence
        lightIntensity = pointLight.intensity;//permet de récupérer l'intensité du prefab directement
        pointLight.intensity = 0f;//lumière éteinte au début de partie
    }

}
