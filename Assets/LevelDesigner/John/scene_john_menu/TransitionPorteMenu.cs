using UnityEngine;
using Unity.Cinemachine;
using System.Collections;

public class TransitionPorteMenu : MonoBehaviour
{
    [Header("1. La Double Porte")]
    public Transform pivotPorteGauche;
    public Transform pivotPorteDroite;
    public Vector3 angleOuvertureGauche = new Vector3(0f, 90f, 0f);
    public Vector3 angleOuvertureDroite = new Vector3(0f, -90f, 0f);
    public float tempsOuverture = 1.5f;

    [Header("2. Le Fondu (Fade)")]
    public CanvasGroup ecranNoir;
    public float tempsFade = 1f;

    [Header("3. La Séquence des Caméras")]
    public SceneMenuNavigator navigator;
    public CinemachineVirtualCameraBase camStart1;    // 1. La caméra qui s'approche de la porte
    public CinemachineVirtualCameraBase camMain1;     // 2. La caméra dans le noir (après le fondu)
    public CinemachineVirtualCameraBase camMainAuto;  // 3. La caméra finale (menu auto)

    [Header("4. Les Timings (Pauses)")]
    public float delaiTrajetVersPorte = 1.5f; // Temps pour que la caméra s'approche avant d'ouvrir
    public float pauseAvantAutoCam = 1f;      // Temps d'attente sur Main 1 avant de glisser vers Main Auto

    private bool sequenceEnCours = false;

    public void LancerLaSequence()
    {
        if (sequenceEnCours) return;
        StartCoroutine(SequenceComplete());
    }

    private IEnumerator SequenceComplete()
    {
        sequenceEnCours = true;

        // ==========================================
        // ACTE 1 : S'APPROCHER DE LA PORTE
        // ==========================================
        if (navigator != null && camStart1 != null) navigator.SwitchToCamera(camStart1);

        // On attend que la caméra arrive devant la porte
        yield return new WaitForSeconds(delaiTrajetVersPorte);


        // ==========================================
        // ACTE 2 : OUVRIR LA PORTE
        // ==========================================
        Quaternion rotationDepartGauche = pivotPorteGauche.rotation;
        Quaternion rotationDepartDroite = pivotPorteDroite.rotation;
        Quaternion rotationFinGauche = pivotPorteGauche.rotation * Quaternion.Euler(angleOuvertureGauche);
        Quaternion rotationFinDroite = pivotPorteDroite.rotation * Quaternion.Euler(angleOuvertureDroite);

        float temps = 0f;
        while (temps < tempsOuverture)
        {
            temps += Time.deltaTime;
            float progression = temps / tempsOuverture;
            pivotPorteGauche.rotation = Quaternion.Slerp(rotationDepartGauche, rotationFinGauche, progression);
            pivotPorteDroite.rotation = Quaternion.Slerp(rotationDepartDroite, rotationFinDroite, progression);
            yield return null;
        }


        // ==========================================
        // ACTE 3 : FONDU AU NOIR ET TELEPORTATION
        // ==========================================
        temps = 0f;
        while (temps < tempsFade)
        {
            temps += Time.deltaTime;
            ecranNoir.alpha = Mathf.Lerp(0f, 1f, temps / tempsFade);
            yield return null;
        }
        ecranNoir.alpha = 1f;

        // On est dans le noir : on saute à la caméra "Main 1"
        if (navigator != null && camMain1 != null) navigator.SwitchToCamera(camMain1);

        // On laisse une demi-seconde à Cinemachine pour s'installer
        yield return new WaitForSeconds(0.5f);


        // ==========================================
        // ACTE 4 : REVENIR À LA LUMIÈRE
        // ==========================================
        temps = 0f;
        while (temps < tempsFade)
        {
            temps += Time.deltaTime;
            ecranNoir.alpha = Mathf.Lerp(1f, 0f, temps / tempsFade);
            yield return null;
        }
        ecranNoir.alpha = 0f;


        // ==========================================
        // ACTE 5 : AUTO-TRANSITION VERS LA FIN
        // ==========================================
        // On admire la vue sur Main 1 un petit instant...
        yield return new WaitForSeconds(pauseAvantAutoCam);

        // ... et on glisse vers la caméra finale Main Auto !
        if (navigator != null && camMainAuto != null) navigator.SwitchToCamera(camMainAuto);

        sequenceEnCours = false;
    }
}