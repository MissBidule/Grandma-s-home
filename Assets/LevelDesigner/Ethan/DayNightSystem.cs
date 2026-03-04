using UnityEngine;

public class DayNightSystem : MonoBehaviour
{
    //variables
    float gameTime = 480f; // 8 minutes de jeu, à changer ou prendre la valeur vers une autre référence si ça change
    
    // positions des axes du soleil par défaut
    float sunInitialX = 60f;
    float sunInitialY = 0f;// (à voir si le temps) randomisation possible entre 0 et 359 pour que le soleil ne se couche pas toujours au même endroit

    float timeBetweenUpdates = 1f; // temps entre chaque mise à jour du placement du soleil
    float additiveAngle = 0.5f; // valeur ajouté à l'angle du soleil à chaque mise à jour

    //note pour jeudi, voir pour gérer l'intensité de la lumière en fonction de l'angle du soleil pour que soit vraiment la nuit quand il est couché
    //voir également la valeur qui fait que c'est lumineux même en changeant ces 2 variables.

    void Start()
    {
        Debug.Log("DayNightSystem started");// test print


    }


    void Update()
    {
        
    }
}
