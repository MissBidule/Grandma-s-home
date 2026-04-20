using UnityEngine;
using System.Collections;
using System.Diagnostics;

public class DayNightSystem : MonoBehaviour
{
    //variables
    private GameObject sun; // référence au soleil dans la scène
    // skybox dual panoramique ref
    public Material skybox; // skybox pour le jour

    private LightOnSystem lightOnSystem; // référence au script d'allumage des lumières
    private bool lightsActivated = false; //si le script d'allumage des lumières a été activé

    private float gameTime = 30f; // 480 - 8 minutes de jeu, prendre la valeur du serveur
    private float currentTime = 0f; // temps actuel dans le cycle jour/nuit
    
    // positions des axes du soleil par défaut
    public float sunInitialX = 150f;
    public float sunInitialY = 0f;
    public bool isRandomSunY = true; // randomiser l'angle y pour différent direction de coucher de soleil
    public float sunIntensity = 3f; // intensité maximale du soleil

    //température du soleil entre le jour et la nuit
    public float temperatureDay = 6000f; 
    public float temperatureNight = 16000f;

    //rotation par actualisation
    public float HdriRotationAngle = 0.1f; 

    private float timeBetweenUpdates = 1f; // temps entre chaque mise à jour des corroutines progressives
    //refreshMultiplier pour que les mise a jour soit plus rapide ou pas sur la même durée de jeu total (plus de fluidité)
    public float refreshMultiplier = 10f;// exemple 1f = 1 seconde | 10f = 0.1 seconde...

    private Coroutine skyCoroutine;

    private float sunRotationAngle; // valeur ajouté à l'angle du soleil à chaque mise à jour
    public Color sunDayColor;// FFE499
    public Color sunNightColor;// 123E41

    //Démarrer avec un angle assez élevé 150, pour la monter à 180 sur 40% du temps de jeu total, faire un changement entre les 2 HDRI blend avec les paramètre de luminosité et allumages progressifs de toutes sources de lumière sur 20% du temps de jeu total, sur les 40% restant de jeu le soleil aura un éclairage d'une couleur plus froide et une intensité plus faible en remontant vers 150 comme une monté de lune.

    void Start()
    {
        UnityEngine.Debug.LogFormat("DayNightSystem started");

        //regarde si l'objet auquel il est attaché a Light
        if (GetComponent<Light>() != null) {
            sun = gameObject;

            InitDayNight();
        }
        else {
            UnityEngine.Debug.LogFormat("DayNightSystem doit être attaché à un objet avec un composant Light !");
        }
    }


    // Initialisation du système de jour/nuit
    void InitDayNight()
    {
        //temps de jeu actuel
        currentTime = 0f;

        //random de l'angle y
        if (isRandomSunY){
            sunInitialY = Random.Range(0f, 360f);
        }

        //rotation du soleil au début
        sun.transform.rotation = Quaternion.Euler(sunInitialX, sunInitialY, 0f);
        // UnityEngine.Debug.LogFormat("Sun initial rotation set to: {0}", sun.transform.rotation.eulerAngles);

        // intensité maximale du soleil au début de partie
        sun.GetComponent<Light>().intensity = sunIntensity;
        // température du soleil au début de partie
        sun.GetComponent<Light>().colorTemperature = temperatureDay;
        //RenderSettings.ambientIntensity à 1 au début de partie
        RenderSettings.ambientIntensity = 1f;

        //angles des 3 texture + blend à 0
        skybox.SetFloat("_Rotation1", 0f);
        skybox.SetFloat("_Rotation2", 0f);
        skybox.SetFloat("_Rotation3", 0f);
        skybox.SetFloat("_Blend", 0f);
        
        //récupère la couleur du soleil au début de partie
        sunDayColor = sun.GetComponent<Light>().color;

        lightOnSystem = GetComponentInParent<LightOnSystem>();//récupère le script d'allumage des lumières dans le parent

        UpdateSky(gameTime);
    }

    //toutes les actualisations a prendre en compte en fonction de l'état du jeu
    void UpdateSky(float serverGameTime)
    {
        gameTime = serverGameTime;

        if (skyCoroutine != null)
        {
            StopCoroutine(skyCoroutine);
        }

        skyCoroutine = StartCoroutine(UpdateSkyCoroutine());
    }

    IEnumerator UpdateSkyCoroutine()
    {
        float updateInterval = timeBetweenUpdates / Mathf.Max(refreshMultiplier, 0.01f);

        while (currentTime < gameTime)
        {
            // UnityEngine.Debug.LogFormat("Game time: {0}", gameTime);
            // UnityEngine.Debug.LogFormat("Updating sky at time: {0}", currentTime);
            //actualisation par défaut
            skybox.SetFloat("_Rotation1", skybox.GetFloat("_Rotation1") + HdriRotationAngle);
            skybox.SetFloat("_Rotation2", skybox.GetFloat("_Rotation2") + HdriRotationAngle);
            skybox.SetFloat("_Rotation3", skybox.GetFloat("_Rotation3") + HdriRotationAngle);

            //coucher du soleil
            if (currentTime < gameTime * 0.4f)
            {
                Sky_SunDown();
            }

            //transition jour-nuit
            else if (currentTime >= gameTime * 0.4f && currentTime < gameTime * 0.6f)
            {
                Sky_DayNightTransition();
                if (!lightsActivated)
                {
                    // Activer les lumières de la scène progressivement sur X secondes via le script LightOnSystem
                    lightOnSystem.TurnOnLights();

                    lightsActivated = true;
                }
            }
            
            // levé de lune
            else if (currentTime >= gameTime * 0.6f)
            {
                Sky_MoonUp();
            }

            currentTime += updateInterval;
            yield return new WaitForSeconds(updateInterval);
        }

        skyCoroutine = null;
    }

    //coucher du soleil
    void Sky_SunDown()
    {
        //rotation du soleil
        //calcul angle de sunInitialX vers 180 degré sur les 40% du temps de jeu total + vérifier que l'angle ne dépasse pas 180
        sunRotationAngle = CheckRotationAngle(sunInitialX + (30f * (currentTime / (gameTime * 0.4f))));
        sun.transform.rotation = Quaternion.Euler(sunRotationAngle, sunInitialY, 0f);

        // Changement progressif du blend de la skybox de 0 à 0.2
        float blend = Mathf.Lerp(0f, 0.2f, currentTime / (gameTime * 0.4f));
        skybox.SetFloat("_Blend", blend);
    }

    //transition jour-nuit
    void Sky_DayNightTransition()
    {
        // Calcul de l'angle du soleil pour 20% du temps de jeu total
        float transitionT = Mathf.Clamp01((currentTime - gameTime * 0.4f) / (gameTime * 0.2f));

        // Changement progressif du blend de la skybox de 0.2 à 0.6
        float blend = Mathf.Lerp(0.2f, 0.6f, transitionT);
        skybox.SetFloat("_Blend", blend);

        //changement progressif de l'intensité du soleil de 3 à 1
        float intensity = Mathf.Lerp(sunIntensity, 1f, transitionT);
        sun.GetComponent<Light>().intensity = intensity;

        //changement progressif de la température du soleil de temperatureDay à temperatureNight
        float temperature = Mathf.Lerp(temperatureDay, temperatureNight, transitionT);
        sun.GetComponent<Light>().colorTemperature = temperature;

        //changement progressif de la couleur du soleil de sunDayColor à sunNightColor
        Color color = Color.Lerp(sunDayColor, sunNightColor, transitionT);
        sun.GetComponent<Light>().color = color;

        //changement progressif de RenderSettings.ambientIntensity de 1 à 0.4
        RenderSettings.ambientIntensity = Mathf.Lerp(1f, 0.4f, transitionT);
    }

    // levé de lune
    void Sky_MoonUp()
    {
        //calcul angle de 180 vers sunInitialX sur les 40% du temps de jeu total + vérifier que l'angle ne dépasse pas sunInitialX
        sunRotationAngle = CheckRotationAngle(180f - (30f * ((currentTime - gameTime * 0.6f) / (gameTime * 0.4f))));
        sun.transform.rotation = Quaternion.Euler(sunRotationAngle, sunInitialY, 0f);

        // Changement progressif du blend de la skybox de 0.6 à 1
        float blend = Mathf.Lerp(0.6f, 1f, (currentTime - gameTime * 0.6f) / (gameTime * 0.4f));
        skybox.SetFloat("_Blend", blend);
    }

    //vérifie si sa dépasse pas les 180 degrés ou le sunInitialX
    float CheckRotationAngle(float angle)
    {
        if (angle > 180f)
        {
            return 180f;
        }
        if (angle < sunInitialX)
        {
            return sunInitialX;
        }
        return angle;
    }
}
