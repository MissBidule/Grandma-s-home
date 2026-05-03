using UnityEngine;

public class LightData : MonoBehaviour
{
    //Light
    [HideInInspector] public float lightIntensity;//intensité cible pour la lumière
    [HideInInspector] public Light pointLight;//référence au composant Light du prefab

    //VFX
    [HideInInspector] public ParticleSystem fireParticleSystem;//particle system - fire
    [HideInInspector] public int fireRateOverTimeMaxEmission;//nombre de particules émises par seconde max pour le feu
    [HideInInspector] public ParticleSystem smokeParticleSystem;//particle system - smoke
    [HideInInspector] public int smokeRateOverTimeMaxEmission;//nombre de particules émises par seconde max pour la fumée

    void Awake() {
        pointLight = GetComponent<Light>();//récupère le component Light en tant que référence
        lightIntensity = pointLight.intensity;//permet de récupérer l'intensité du prefab directement
        pointLight.intensity = 0f;//lumière éteinte au début de partie

        // Cherche et initialise le fire VFX
        Transform fireTransform = transform.Find("VFX_Fire");
        if (fireTransform != null) {
            fireParticleSystem = fireTransform.GetComponent<ParticleSystem>();
            if (fireParticleSystem != null) {
                // Récupère le max emission rate avant de le mettre à 0
                ParticleSystem.EmissionModule fireEmission = fireParticleSystem.emission;
                fireRateOverTimeMaxEmission = (int)fireEmission.rateOverTime.constant;
                fireEmission.rateOverTime = 0f;
                // Cache le VFX
                fireParticleSystem.gameObject.SetActive(false);
            }
        }

        // Cherche et initialise le smoke VFX
        Transform smokeTransform = transform.Find("VFX_Smoke");
        if (smokeTransform != null) {
            smokeParticleSystem = smokeTransform.GetComponent<ParticleSystem>();
            if (smokeParticleSystem != null) {
                // Récupère le max emission rate avant de le mettre à 0
                ParticleSystem.EmissionModule smokeEmission = smokeParticleSystem.emission;
                smokeRateOverTimeMaxEmission = (int)smokeEmission.rateOverTime.constant;
                smokeEmission.rateOverTime = 0f;
                // Cache le VFX
                smokeParticleSystem.gameObject.SetActive(false);
            }
        }
    }

}
