using UnityEngine;
using Unity.Cinemachine; // Obligatoire pour Unity 6
using System.Collections;

public class MoteurSpline : MonoBehaviour
{
    [Header("Glisse le composant Spline Dolly ici")]
    public CinemachineSplineDolly dolly;

    [Header("Temps pour aller jusqu'à la chambre (en secondes)")]
    public float dureeTrajet = 2f;

    // C'est CETTE fonction qu'on va appeler avec le bouton
    public void LancerLeTrajet()
    {
        // Si la case dolly est vide, le script essaie de la trouver tout seul
        if (dolly == null)
        {
            dolly = GetComponent<CinemachineSplineDolly>();
        }

        // On arrête les anciens mouvements et on lance le nouveau
        StopAllCoroutines();
        StartCoroutine(AvancerSurLeRail());
    }

    private IEnumerator AvancerSurLeRail()
    {
        float tempsEcoule = 0f;

        // On force la caméra à se mettre au tout début du rail (0)
        dolly.CameraPosition = 0f;

        // Boucle qui fait avancer la caméra frame par frame
        while (tempsEcoule < dureeTrajet)
        {
            tempsEcoule += Time.deltaTime;

            // SmoothStep permet un démarrage et un freinage en douceur
            dolly.CameraPosition = Mathf.SmoothStep(0f, 1f, tempsEcoule / dureeTrajet);

            yield return null; // Attend l'image suivante
        }

        // On s'assure qu'elle est bien arrivée tout au bout (1)
        dolly.CameraPosition = 1f;
    }
}