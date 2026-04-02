using UnityEngine;
using Unity.Cinemachine;
using System.Collections;

public class TransitionDoublePorteMenu : MonoBehaviour
{
    [Header("1. Double Porte")] //ref pivots des portes
    public Transform pivotPorteGauche; 
    public Transform pivotPorteDroite;
    public Vector3 angleOuvertureGauche = new Vector3(0f, 90f, 0f); 
    public Vector3 angleOuvertureDroite = new Vector3(0f, -90f, 0f);
    public float tempsOuverture = 1.5f; //durée de l'ouverture des portes

    [Header("2. Transition Fade")]
    public CanvasGroup ecranNoir; //ref canva Canvas_Fade
    public float tempsFade = 1.5f; //durée du fade to black
    public float tempsFadeIn = 0.5f; //durée du fade in après le fade to black

    [Header("3. Caméras")]
    public SceneMenuNavigator navigator; //ref menu manager
    public CinemachineVirtualCameraBase camDepart; //ref cam VCam_Start_Zoom_Door
    public CinemachineVirtualCameraBase camSequencer; //ref cam Sequencer Camera Main
    public CinemachineVirtualCameraBase camMainFinale; //ref cam VCam_Main

    [Header("4. Réglages")] //divers pour ajuster le timing de l'intro
    public float tempsDansLeNoir = 1f; //durée pendant laquelle l'écran reste noir avant de faire le fade in
    public float dureeZoomSequencer = 3.0f; //durée zoom avant de switch à la cam finale après le fade in

    [Header("5. Démarrage")]
    [Tooltip("Temps en secondes pendant lequel le joueur ne peut pas cliquer au tout début du jeu")]
    public float delaiAvantAutoriserClic = 3.0f; //evite que le joueur skip l'intro mettre a 0 sinon
    private float chronometreDemarrage = 0f;

    private bool sequenceEnCours = false;//evite de lancer plusieurs fois la séquence si le joueur clique spam pendant l'intro
    private bool introDejaJouee = false; //intro ne se joue qu'une fois même si le joueur clique plusieurs fois

    private void Update()
    {

        chronometreDemarrage += Time.deltaTime;
        //bloque clicks 
        if (chronometreDemarrage < delaiAvantAutoriserClic)
        {
            return;
        }
        //press any key pour lancer l'intro
        if (!introDejaJouee && Input.anyKeyDown)
        {
            introDejaJouee = true;
            LancerLaSequence();
        }
    }
    //lance la coroutine de la séquence d'intro
    public void LancerLaSequence()
    {
        if (sequenceEnCours) return;
        StartCoroutine(SequenceComplete());
    }

    //coroutine avec étapes séqeunce intro
    private IEnumerator SequenceComplete()
    {
        //1. ouvrir les portes
        sequenceEnCours = true;
        if (navigator != null && camDepart != null) navigator.SwitchToCamera(camDepart);
        Quaternion rotationDepartGauche = pivotPorteGauche.rotation;
        Quaternion rotationDepartDroite = pivotPorteDroite.rotation;
        Quaternion rotationFinGauche = pivotPorteGauche.rotation * Quaternion.Euler(angleOuvertureGauche);
        Quaternion rotationFinDroite = pivotPorteDroite.rotation * Quaternion.Euler(angleOuvertureDroite);

        float tempsMax = Mathf.Max(tempsOuverture, tempsFade);
        float temps = 0f;

        //2. faire le fade to black
        while (temps < tempsMax)
        {
            temps += Time.deltaTime;
            float progressionPorte = Mathf.Clamp01(temps / tempsOuverture);
            pivotPorteGauche.rotation = Quaternion.Slerp(rotationDepartGauche, rotationFinGauche, progressionPorte);
            pivotPorteDroite.rotation = Quaternion.Slerp(rotationDepartDroite, rotationFinDroite, progressionPorte);

            float progressionFade = Mathf.Clamp01(temps / tempsFade);
            if (ecranNoir != null) ecranNoir.alpha = Mathf.Lerp(0f, 1f, progressionFade);

            yield return null;
        }

        pivotPorteGauche.rotation = rotationFinGauche;
        pivotPorteDroite.rotation = rotationFinDroite;
        if (ecranNoir != null) ecranNoir.alpha = 1f;

        //3. switch cam sequencer
        if (navigator != null && camSequencer != null)
        {
            navigator.SwitchToCamera(camSequencer);
        }

        yield return new WaitForSeconds(tempsDansLeNoir);


        temps = 0f;
        //4. faire le fade in
        while (temps < tempsFadeIn)
        {
            temps += Time.deltaTime;
            if (ecranNoir != null) ecranNoir.alpha = Mathf.Lerp(1f, 0f, temps / tempsFadeIn);
            yield return null;
        }
        if (ecranNoir != null) ecranNoir.alpha = 0f;

        //5. switch cam finale
        yield return new WaitForSeconds(dureeZoomSequencer);

        if (navigator != null && camMainFinale != null)
        {
            navigator.SwitchToCamera(camMainFinale);
        }

        sequenceEnCours = false;
    }
}