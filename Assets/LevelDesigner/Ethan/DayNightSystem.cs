using UnityEngine;
using System.Collections;
using System.Diagnostics;

public class DayNightSystem : MonoBehaviour
{
    //variables
    private GameObject sun; // référence au soleil dans la scène
    // skybox dual panoramique ref
    public Material skybox; // skybox pour le jour

    public float gameTime = 480f; // 480 - 8 minutes de jeu, à changer ou prendre la valeur vers une autre référence si ça change
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
    public float refreshMultiplier = 2f;

    private float timeBetweenGeneralUpdates = 0.1f; // temps entre chaque mise à jour des éléments généraux

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
        UnityEngine.Debug.LogFormat("Sun initial rotation set to: {0}", sun.transform.rotation.eulerAngles);

        // intensité maximale du soleil au début de partie
        sun.GetComponent<Light>().intensity = sunIntensity;
        // température du soleil au début de partie
        sun.GetComponent<Light>().colorTemperature = temperatureDay;
        //RenderSettings.ambientIntensity à 1 au début de partie
        RenderSettings.ambientIntensity = 1f;

        //angles des 2 texture + blend à 0
        skybox.SetFloat("_Rotation1", 0f);
        skybox.SetFloat("_Rotation2", 0f);
        skybox.SetFloat("_Blend", 0f);
        
        //récupère la couleur du soleil au début de partie
        sunDayColor = sun.GetComponent<Light>().color;


        //démarrage des coroutines
        StartCoroutine(GeneralUpdates());
        StartCoroutine(UpdateLight_LowerSun());
    }

    //vérifie si sa dépasse pas les 180 degrés
    float CheckRotationAngle(float angle)
    {
        if (angle > 180f)
        {
            return 180f;
        }
        return angle;
    }

    IEnumerator UpdateLight_LowerSun()
    {//début de la partie, le soleil se couche
        while (currentTime < gameTime * 0.4f)
        {
            //rotation du soleil
            //calcul angle de sunInitialX vers 180 degré sur les 40% du temps de jeu total + vérifier que l'angle ne dépasse pas 180
            sunRotationAngle = CheckRotationAngle(sunInitialX + (30f * (currentTime / (gameTime * 0.4f))));
            sun.transform.rotation = Quaternion.Euler(sunRotationAngle, sunInitialY, 0f);

            // incrémente le temps actuel
            currentTime += timeBetweenUpdates; 
            yield return new WaitForSeconds(timeBetweenUpdates);// attend entre chaque update
        }
        //démarrage de la seconde corroutine pour la transition jour-nuit
        StartCoroutine(UpdateLight_Transition());
    }

    IEnumerator UpdateLight_Transition() // transition entre le jour et la nuit
    {
        while (currentTime >= gameTime * 0.4f && currentTime < gameTime * 0.6f)
        {
            //blend progressif des 2 texture de la skybox
            float blend = Mathf.Lerp(0f, 1f, (currentTime - gameTime * 0.4f) / (gameTime * 0.2f));
            skybox.SetFloat("_Blend", blend);

            //changement progressif de l'intensité du soleil de 3 à 1
            float intensity = Mathf.Lerp(sunIntensity, 1f, (currentTime - gameTime * 0.4f) / (gameTime * 0.2f));
            sun.GetComponent<Light>().intensity = intensity;

            //changement progressif de la température du soleil de temperatureDay à temperatureNight
            float temperature = Mathf.Lerp(temperatureDay, temperatureNight, (currentTime - gameTime * 0.4f) / (gameTime * 0.2f));
            sun.GetComponent<Light>().colorTemperature = temperature;

            //changement progressif de la couleur du soleil de sunDayColor à sunNightColor
            Color color = Color.Lerp(sunDayColor, sunNightColor, (currentTime - gameTime * 0.4f) / (gameTime * 0.2f));
            sun.GetComponent<Light>().color = color;

            //changement progressif de RenderSettings.ambientIntensity de 1 à 0.4
            RenderSettings.ambientIntensity = Mathf.Lerp(1f, 0.4f, (currentTime - gameTime * 0.3f) / (gameTime * 0.2f));
            

            currentTime += timeBetweenUpdates;
            yield return new WaitForSeconds(timeBetweenUpdates);
        }
        //démarrage de la troisième corroutine pour la montée de la lune
        StartCoroutine(UpdateLight_UpperMoon());
    }

    IEnumerator UpdateLight_UpperMoon() // montée de la lune
    {
        while (currentTime >= gameTime * 0.6f && currentTime < gameTime)
        {
            //rotation du soleil
            //calcul angle de 180 vers sunInitialX sur les 40% du temps de jeu total + vérifier que l'angle ne dépasse pas sunInitialX
            sunRotationAngle = CheckRotationAngle(180f - (30f * ((currentTime - gameTime * 0.6f) / (gameTime * 0.4f))));
            sun.transform.rotation = Quaternion.Euler(sunRotationAngle, sunInitialY, 0f);

            currentTime += timeBetweenUpdates;
            yield return new WaitForSeconds(timeBetweenUpdates);
        }
    }
    
    
    IEnumerator GeneralUpdates() // update des éléments généraux hors périodes spécifiques
    {
        while (currentTime < gameTime)
        {
            //rotation des 2 texture de la skybox
            skybox.SetFloat("_Rotation1", skybox.GetFloat("_Rotation1") + HdriRotationAngle);
            skybox.SetFloat("_Rotation2", skybox.GetFloat("_Rotation2") + HdriRotationAngle);

            yield return new WaitForSeconds(timeBetweenGeneralUpdates);
        }
    }
}
