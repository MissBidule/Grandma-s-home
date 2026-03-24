using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class LightOnSystem : MonoBehaviour
{
    public float LightOnDuration = 10f;
    [HideInInspector] public List<LightData> lights;

    private bool running = true;

    void Start() {
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
        float elapsed = 0f;
        while (elapsed < LightOnDuration) {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / LightOnDuration);
            foreach (var ld in lights) {
                ld.pointLight.intensity = Mathf.Lerp(0f, ld.lightIntensity, t);
            }
            yield return null;
        }
    }
}
