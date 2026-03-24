using UnityEngine;
using Unity.Cinemachine;
using System.Collections;

public class MoteurSpline : MonoBehaviour
{
    [Header("La Caméra qui fait le trajet")]
    public CinemachineSplineDolly dolly;
    public float dureeTrajet = 2f;

    [Header("Pour la transition finale")]
    public SceneMenuNavigator navigator;
    public CinemachineVirtualCameraBase Cam;

    // Variable secrète pour manipuler cette caméra
    private CinemachineVirtualCameraBase maCameraSpline;

    private void Start()
    {
        // Le script trouve tout seul la caméra sur laquelle il est posé
        maCameraSpline = GetComponent<CinemachineVirtualCameraBase>();
    }

    public void LancerLeTrajet()
    {
        if (dolly == null) dolly = GetComponent<CinemachineSplineDolly>();
        StopAllCoroutines();

        // 1. LE ROULEAU COMPRESSEUR : On met la priorité à 30 !
        // Ça force Unity à afficher cette caméra, peu importe les bugs du menu.
        if (maCameraSpline != null)
        {
            maCameraSpline.Priority = 30;
        }

        StartCoroutine(Trajet());
    }

    private IEnumerator Trajet()
    {
        float temps = 0f;

        // On rembobine bien l'animation à zéro
        dolly.CameraPosition = 0f;

        // On fait avancer la caméra
        while (temps < dureeTrajet)
        {
            temps += Time.deltaTime;
            dolly.CameraPosition = Mathf.SmoothStep(0f, 1f, temps / dureeTrajet);
            yield return null;
        }

        // On s'assure d'être à la fin
        dolly.CameraPosition = 1f;

        // 2. FIN DU TRAJET : On éteint cette caméra en la remettant à 10
        if (maCameraSpline != null)
        {
            maCameraSpline.Priority = 10;
        }

        // 3. On demande au Navigator d'allumer la chambre (qui passera à 20)
        if (navigator != null && Cam != null)
        {
            navigator.SwitchToCamera(Cam);
        }
    }
}