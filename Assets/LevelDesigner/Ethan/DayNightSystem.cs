using UnityEngine;
using System.Collections;

public class DayNightSystem : MonoBehaviour
{
    //variables
    private GameObject sun; // référence au soleil dans la scène

    public float gameTime = 480f; // 8 minutes de jeu, à changer ou prendre la valeur vers une autre référence si ça change
    [SerializeField] private float currentTime = 0f; // temps actuel dans le cycle jour/nuit
    
    // positions des axes du soleil par défaut
    public float sunInitialX = 60f;
    public float sunInitialY = 0f;
    public bool isRandomSunY = true; // randomiser l'angle y pour différent direction de coucher de soleil
    public float sunIntensity = 3f; // intensité maximale du soleil

    public float timeBetweenUpdates = 0.2f; // temps entre chaque mise à jour du placement du soleil
    public float additiveAngle = 0.1f; // valeur ajouté à l'angle du soleil à chaque mise à jour

    //Démarrer avec un angle assez élevé 150, pour la monter à 170 sur 40% du temps de jeu total, faire un changement entre les 2 HDRI blend avec les paramètre de luminosité et allumages progressifs de toutes sources de lumière sur 20% du temps de jeu total, sur les 40% restant de jeu le soleil aura un éclairage d'une couleur plus froide et une intensité plus faible en remontant vers 150 comme une monté de lune.
    //Voir la rotation du HDRI pour que la directional light avec le random en Y ne rentre pas en collision avec ses nuages 2D


    //VIEUX - NON FONCTIONNEL
    //2 secondes = 1 angle
    //1 minute = 30 angle
    //De 4 minutes à 6 minutes = 120 angle | l'intensité de la lumière va diminuer jusqu'a 0 
    //De 6 minutes à 7 minutes = 180 angle | allumage des sources de lumières dans la map et essayer d'assombrir encore plus la lumière de la skybox pendant 1 minute
    //8 minutes = 240 angle 
    void Start()
    {
        Debug.Log("DayNightSystem started");

        //regarde si l'objet auquel il est attaché a Light
        if (GetComponent<Light>() != null) {
            sun = gameObject;

            InitDayNight();
            
            //démarrage de la coroutine
            StartCoroutine(UpdateSun());

        }
        else {
            Debug.LogError("DayNightSystem doit être attaché à un objet avec un composant Light !");
        }
    }


    // Initialisation du système de jour/nuit
    void InitDayNight()
    {
        currentTime = 0f;
        sun.GetComponent<Light>().intensity = sunIntensity; // intensité maximale du soleil au début

        // Position initiale du soleil
        if (isRandomSunY){//random de l'angle y
        sunInitialY = Random.Range(0f, 360f);
        }
        sun.transform.rotation = Quaternion.Euler(sunInitialX, sunInitialY, 0f);
    }

    IEnumerator UpdateSun() // update de la position du soleil
    {
        while (currentTime < gameTime)
        {
            sun.transform.Rotate(additiveAngle, 0f, 0f); // fait tourner le soleil autour de l'axe x

            //Change l'intensité si l'angle est entre 120 et 180 de 3 à 0
            if (sun.transform.rotation.eulerAngles.x > 120f && sun.transform.rotation.eulerAngles.x < 180f) {
                float intensity = Mathf.Lerp(sunIntensity, 0f, (sun.transform.rotation.eulerAngles.x - 120f) / 60f); // calcul de l'intensité en fonction de l'angle
                sun.GetComponent<Light>().intensity = intensity; // applique l'intensité calculée
            }
            else if (sun.transform.rotation.eulerAngles.x >= 180f) {
                sun.GetComponent<Light>().intensity = 0f; // éteint la lumière du soleil
            }

            currentTime += timeBetweenUpdates; // incrémente le temps actuel
            yield return new WaitForSeconds(timeBetweenUpdates);// attend entre chaque update
        }
    }
    
    // quand l'angle > une valeur proche de 180, réduire l'intensité de la lumière jusqu'a 0 pour que au dela de 180, ça éclaire pas le dessous de la map et voir pour diminuer toute AUTRES lumière causé pas la skybox qui peut rendre la nuit encore trop lumineux.
}
