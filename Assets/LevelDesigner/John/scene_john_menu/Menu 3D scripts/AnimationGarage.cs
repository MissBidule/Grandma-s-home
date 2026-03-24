using UnityEngine;
using System.Collections;

public class AnimationGarage : MonoBehaviour
{
    [Header("Réglages de l'Animation")]
    public float distanceOuverture = 3.5f;

    [Tooltip("Temps pour monter")]
    public float tempsMontee = 1.0f;

    [Tooltip("Temps d'attente en haut avant de se refermer automatiquement")]
    public float tempsAttenteEnHaut = 1.5f; // 🎯 Le temps que ta caméra passe !

    [Tooltip("Temps pour redescendre")]
    public float tempsDescente = 1.0f;

    private Vector3 positionInitiale;
    private bool enMouvement = false;

    private void Start()
    {
        // On mémorise la position de départ une seule fois au lancement
        positionInitiale = transform.position;
    }

    // Détecte le clic de souris directement sur l'objet 3D
    private void OnMouseDown()
    {
        // Si la porte bouge déjà, on ignore le clic
        if (!enMouvement)
        {
            StartCoroutine(SequenceOuvertureFermeture());
        }
    }

    private IEnumerator SequenceOuvertureFermeture()
    {
        enMouvement = true;
        Vector3 positionHaute = positionInitiale + (transform.up * distanceOuverture);

        // =====================================
        // 1. LA PORTE MONTE
        // =====================================
        float temps = 0f;
        while (temps < tempsMontee)
        {
            temps += Time.deltaTime;
            float progression = temps / tempsMontee;
            transform.position = Vector3.Lerp(positionInitiale, positionHaute, Mathf.SmoothStep(0f, 1f, progression));
            yield return null;
        }
        transform.position = positionHaute; // On s'assure qu'elle est bien calée en haut

        // =====================================
        // 2. PAUSE (La porte reste ouverte)
        // =====================================
        yield return new WaitForSeconds(tempsAttenteEnHaut);

        // =====================================
        // 3. LA PORTE REDESCEND
        // =====================================
        temps = 0f;
        while (temps < tempsDescente)
        {
            temps += Time.deltaTime;
            float progression = temps / tempsDescente;
            transform.position = Vector3.Lerp(positionHaute, positionInitiale, Mathf.SmoothStep(0f, 1f, progression));
            yield return null;
        }
        transform.position = positionInitiale; // On s'assure qu'elle est parfaitement fermée

        enMouvement = false; // Le cycle est fini, on peut recliquer !
    }
}