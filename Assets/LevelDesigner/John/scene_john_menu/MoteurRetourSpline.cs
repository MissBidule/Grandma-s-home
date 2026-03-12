using UnityEngine;
using Unity.Cinemachine;
using System.Collections;

public class MoteurRetourSpline : MonoBehaviour
{
    [Header("La Caméra qui descend")]
    public CinemachineSplineDolly dolly;
    public float dureeTrajet = 2f;

    [Header("Pour la transition finale")]
    public SceneMenuNavigator navigator; // Ton script qui gère les caméras
    public CinemachineVirtualCameraBase mainCam; // La caméra du menu principal

    public void LancerLaDescente()
    {
        if (dolly == null) dolly = GetComponent<CinemachineSplineDolly>();
        StopAllCoroutines();
        StartCoroutine(TrajetDescente());
    }

    private IEnumerator TrajetDescente()
    {
        float temps = 0f;

        // On place la caméra au début de ta 2ème Spline (en haut)
        dolly.CameraPosition = 0f;

        // On fait avancer la caméra jusqu'en bas
        while (temps < dureeTrajet)
        {
            temps += Time.deltaTime;
            dolly.CameraPosition = Mathf.SmoothStep(0f, 1f, temps / dureeTrajet);
            yield return null;
        }

        // On s'assure d'être exactement à la fin du rail (en bas)
        dolly.CameraPosition = 1f;

        // 🎯 L'ACTION FINALE : On rebascule sur le menu principal !
        if (navigator != null && mainCam != null)
        {
            navigator.SwitchToCamera(mainCam);
        }
    }
}