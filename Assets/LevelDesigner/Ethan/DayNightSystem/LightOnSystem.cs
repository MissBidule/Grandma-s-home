using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class LightOnSystem : MonoBehaviour
{
    public float LightOnDuration = 10f;
    [HideInInspector] public List<LightData> lights;

    private bool running = true;

    void Start() {
        lights = new List<LightData>();
        //trouve tout les pointlight avec le script LightData pour les ajouter à la liste
        LightData[] allLights = FindObjectsByType<LightData>(FindObjectsSortMode.None);
        foreach (var ld in allLights) {
            lights.Add(ld);
        }

        //print les éléments de la liste pour vérification
        // foreach (var ld in lights) {
        //     UnityEngine.Debug.LogFormat("Light found: {0} with intensity {1}", ld.gameObject.name, ld.lightIntensity);
        // }
    }

    public void TurnOnLights() {
        if (!running) return;
        StartCoroutine(LightsOn());
    }

    IEnumerator LightsOn()
    {
        //activation des vfx de feu et de fumée
        foreach (var ld in lights) {
            if (ld.fireParticleSystem != null) {
                ld.fireParticleSystem.gameObject.SetActive(true);
                ld.fireParticleSystem.Play();
            }
            if (ld.smokeParticleSystem != null) {
                ld.smokeParticleSystem.gameObject.SetActive(true);
                ld.smokeParticleSystem.Play();
            }
        }
        float elapsed = 0f;
        while (elapsed < LightOnDuration) {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / LightOnDuration);
            foreach (var ld in lights) {
                // Augmente progressivement l'intensité de la lumière
                ld.pointLight.intensity = Mathf.Lerp(0f, ld.lightIntensity, t);

                // Augmente progressivement le rate over time du fire VFX
                if (ld.fireParticleSystem != null) {
                    ParticleSystem.EmissionModule fireEmission = ld.fireParticleSystem.emission;
                    fireEmission.rateOverTime = Mathf.Lerp(1f, ld.fireRateOverTimeMaxEmission, t);//mathlerp à 1 pour que les particules commencent à être émises dès le début sinon la lumière va s'allumer plus vite que les particules
                }

                // Augmente progressivement le rate over time du smoke VFX
                if (ld.smokeParticleSystem != null) {
                    ParticleSystem.EmissionModule smokeEmission = ld.smokeParticleSystem.emission;
                    smokeEmission.rateOverTime = Mathf.Lerp(1f, ld.smokeRateOverTimeMaxEmission, t);
                }
            }
            yield return null;
        }
    }
}
