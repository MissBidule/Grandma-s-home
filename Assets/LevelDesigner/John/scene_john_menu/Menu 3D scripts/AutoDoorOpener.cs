using UnityEngine;
using System.Collections;

public class AutoDoorOpener : MonoBehaviour
{
    [Header("Réglages de la Porte")]
    public Transform pivotPorteGauche;
    public Transform pivotPorteDroite;
    public Vector3 angleOuvertureGauche = new Vector3(0, 90, 0);
    public Vector3 angleOuvertureDroite = new Vector3(0, -90, 0);

    [Header("Timing")]
    public float delaiAvantOuverture = 2.5f; // Attend que le zoom commence
    public float tempsOuverture = 1.5f;

    void Start()
    {
        // Se lance tout seul au démarrage du jeu
        StartCoroutine(OuvrirPorteAuto());
    }

    IEnumerator OuvrirPorteAuto()
    {
        // 1. Attente du moment précis dans ta séquence
        yield return new WaitForSeconds(delaiAvantOuverture);

        // 2. Animation d'ouverture
        float temps = 0f;
        Quaternion startRotG = pivotPorteGauche.localRotation;
        Quaternion startRotD = pivotPorteDroite.localRotation;
        Quaternion endRotG = startRotG * Quaternion.Euler(angleOuvertureGauche);
        Quaternion endRotD = startRotD * Quaternion.Euler(angleOuvertureDroite);

        while (temps < tempsOuverture)
        {
            temps += Time.deltaTime;
            float p = temps / tempsOuverture;

            pivotPorteGauche.localRotation = Quaternion.Slerp(startRotG, endRotG, p);
            pivotPorteDroite.localRotation = Quaternion.Slerp(startRotD, endRotD, p);
            yield return null;
        }
    }
}