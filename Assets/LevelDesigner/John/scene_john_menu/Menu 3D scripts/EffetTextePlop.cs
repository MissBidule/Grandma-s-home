using UnityEngine;
using TMPro; // 🎯 Indispensable pour parler à TextMeshPro
using System.Collections;

public class EffetTextePlop : MonoBehaviour
{
    [Header("Réglages de l'Effet (Pâte/Papier)")]
    [Tooltip("Le temps que met le texte à s'ouvrir")]
    public float dureeApparition = 0.6f;

    [Tooltip("L'angle de travers au départ pour l'effet 'froissé'")]
    public float angleDeDepart = 45f;

    private Vector3 echelleInitiale;
    private Quaternion rotationInitiale;

    // 🎯 La courbe magique ! Elle fait "0 -> 1.2 -> 1" pour donner l'effet de rebond élastique
    private AnimationCurve courbeRebond = new AnimationCurve(
        new Keyframe(0f, 0f),
        new Keyframe(0.7f, 1.2f), // Le texte grossit un peu trop...
        new Keyframe(1f, 1f)      // ...puis reprend sa taille normale !
    );

    private void Awake()
    {
        // On mémorise comment tu as placé ton texte dans la scène
        echelleInitiale = transform.localScale;
        rotationInitiale = transform.rotation;

        // On le cache (taille zéro) au démarrage du jeu
        transform.localScale = Vector3.zero;
        gameObject.SetActive(false);
    }

    public void Apparaitre()
    {
        if (gameObject.activeSelf) return; // Sécurité s'il est déjà là

        gameObject.SetActive(true);
        StopAllCoroutines();
        StartCoroutine(AnimerTexte());
    }

    private IEnumerator AnimerTexte()
    {
        float temps = 0f;

        // On crée une rotation "tordue" de départ (comme une boule froissée)
        Quaternion rotationFroissee = rotationInitiale * Quaternion.Euler(0, 0, angleDeDepart);

        while (temps < dureeApparition)
        {
            temps += Time.deltaTime;
            float progression = temps / dureeApparition;

            // On lit notre courbe magique pour avoir le rebond
            float valeurCourbe = courbeRebond.Evaluate(progression);

            // 1. L'effet "Pâte qui gonfle"
            transform.localScale = echelleInitiale * valeurCourbe;

            // 2. L'effet "Papier qui se défroisse" (il se remet droit)
            transform.rotation = Quaternion.Lerp(rotationFroissee, rotationInitiale, progression);

            yield return null;
        }

        // On s'assure qu'il est parfait à la fin
        transform.localScale = echelleInitiale;
        transform.rotation = rotationInitiale;
    }
}