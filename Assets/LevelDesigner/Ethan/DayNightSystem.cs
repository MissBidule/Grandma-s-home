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
    [SerializeField] private float currentTime = 0f; // temps actuel dans le cycle jour/nuit
    
    // positions des axes du soleil par défaut
    public float sunInitialX = 150f;
    public float sunInitialY = 0f;
    public bool isRandomSunY = true; // randomiser l'angle y pour différent direction de coucher de soleil
    public float sunIntensity = 3f; // intensité maximale du soleil

    //température du soleil entre le jour et la nuit
    public float temperatureDay = 6000f; 
    public float temperatureNight = 16000f;

    public float HdriRotationAngle = 0.05f; //rotation par actualisation
    public float HdriRotationSpeed = 0.1f; // vitesse de rotation de la skybox

    private float timeBetweenUpdates = 1f; // temps entre chaque mise à jour du placement du soleil
    private float sunRotationAngle; // valeur ajouté à l'angle du soleil à chaque mise à jour

    //Démarrer avec un angle assez élevé 150, pour la monter à 180 sur 40% du temps de jeu total, faire un changement entre les 2 HDRI blend avec les paramètre de luminosité et allumages progressifs de toutes sources de lumière sur 20% du temps de jeu total, sur les 40% restant de jeu le soleil aura un éclairage d'une couleur plus froide et une intensité plus faible en remontant vers 150 comme une monté de lune.

    void Start()
    {
        UnityEngine.Debug.LogFormat("DayNightSystem started");

        //regarde si l'objet auquel il est attaché a Light
        if (GetComponent<Light>() != null) {
            sun = gameObject;

            InitDayNight();
            
            //démarrage des coroutines
            StartCoroutine(SkyboxRotation());
            StartCoroutine(UpdateLight_LowerSun());

        }
        else {
            UnityEngine.Debug.LogFormat("DayNightSystem doit être attaché à un objet avec un composant Light !");
        }
    }


    // Initialisation du système de jour/nuit
    void InitDayNight()
    {
        currentTime = 0f;//temps de jeu actuel
        // Position initiale du soleil
        if (isRandomSunY){//random de l'angle y
        sunInitialY = Random.Range(0f, 360f);
        }
        UnityEngine.Debug.LogFormat("Sun initial X angle: {0}, Sun initial Y angle: {1}", sunInitialX, sunInitialY);
        sun.transform.rotation = Quaternion.Euler(sunInitialX, sunInitialY, 0f);
        // sun.transform.rotation = Quaternion.Euler(150, 0, 0f);

        UnityEngine.Debug.LogFormat("Sun initial rotation set to: {0}", sun.transform.rotation.eulerAngles);

        sun.GetComponent<Light>().intensity = sunIntensity; // intensité maximale du soleil au début
        sun.GetComponent<Light>().colorTemperature = temperatureDay; // température du soleil du début de partie

        //angles des 2 texture + blend à 0
        skybox.SetFloat("_Rotation1", 0f);
        skybox.SetFloat("_Rotation2", 0f);
        skybox.SetFloat("_Blend", 0f);

        //calcul de l'angle de rotation du soleil pour allez de sunInitialX à 180 sur 40% du temps de jeu total
        sunRotationAngle = (180f - sunInitialX) / (gameTime * 0.4f);
        UnityEngine.Debug.LogFormat("Sun rotation angle calculated: {0}", sunRotationAngle);

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
            float newSunX = CheckRotationAngle(sun.transform.rotation.eulerAngles.x + sunRotationAngle);//vérifie que l'angle ne dépasse pas 180
            sun.transform.rotation = Quaternion.Euler(newSunX, sun.transform.rotation.eulerAngles.y, 0f);

            currentTime += timeBetweenUpdates; // incrémente le temps actuel
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

            //changement progressif de l'intensité du soleil
            float intensity = Mathf.Lerp(0f, sunIntensity * 0.5f, (currentTime - gameTime * 0.4f) / (gameTime * 0.2f));
            sun.GetComponent<Light>().intensity = intensity;

            //changement progressif de la température du soleil
            float temperature = Mathf.Lerp(temperatureDay, temperatureNight, (currentTime - gameTime * 0.4f) / (gameTime * 0.2f));
            sun.GetComponent<Light>().colorTemperature = temperature;

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
            yield return new WaitForSeconds(timeBetweenUpdates);
        }
    }
    
    
    IEnumerator SkyboxRotation() // update de la rotation de la skybox
    {
        while (currentTime < gameTime)
        {
            //rotation des 2 texture de la skybox
            skybox.SetFloat("_Rotation1", skybox.GetFloat("_Rotation1") + HdriRotationAngle);
            skybox.SetFloat("_Rotation2", skybox.GetFloat("_Rotation2") + HdriRotationAngle);

            yield return new WaitForSeconds(timeBetweenUpdates);
        }
    }
}
