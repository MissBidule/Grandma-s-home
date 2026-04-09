using UnityEngine;
using System.Collections;

public class AutoDoorOpener : MonoBehaviour
{
    //script pour les Portail_Double
    [Header("Réglages du Portail")]
    public Transform pivotPorteGauche; //ref pivot du portail gauche
    public Transform pivotPorteDroite; //ref pivot du la portail droite 
    public Vector3 angleOuvertureGauche = new Vector3(0, 90, 0); //angle et ajustable 90 pour ouverture ext et 
    public Vector3 angleOuvertureDroite = new Vector3(0, -90, 0); //-90 pour ouverture int

    [Header("Timing")]
    public float delaiAvantOuverture = 2.5f; //param pour le timing des portails pendant l'intro
    public float tempsOuverture = 1.5f; //durée de l'ouverture des portails pour que ce soit fluide

    void Start()
    {
        StartCoroutine(OuvrirPorteAuto());
    }

    //coroutine pour ouvrir les portails automatiquement après un délai
    IEnumerator OuvrirPorteAuto()
    {
        yield return new WaitForSeconds(delaiAvantOuverture);

        float temps = 0f;
        Quaternion startRotG = pivotPorteGauche.localRotation;
        Quaternion startRotD = pivotPorteDroite.localRotation;
        Quaternion endRotG = startRotG * Quaternion.Euler(angleOuvertureGauche);
        Quaternion endRotD = startRotD * Quaternion.Euler(angleOuvertureDroite);

       
        while (temps < tempsOuverture)
        {
            temps += Time.deltaTime;
            float p = temps / tempsOuverture;
            //interpolation pour ouvrir les portes de manière fluide
            pivotPorteGauche.localRotation = Quaternion.Slerp(startRotG, endRotG, p);
            pivotPorteDroite.localRotation = Quaternion.Slerp(startRotD, endRotD, p);
            yield return null;
        }
    }
}