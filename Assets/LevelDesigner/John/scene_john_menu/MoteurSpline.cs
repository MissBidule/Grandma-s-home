using UnityEngine;
using Unity.Cinemachine;
using System.Collections;

public class MoteurSpline : MonoBehaviour
{
    [Header("La Caméra qui fait le trajet")]
    public CinemachineSplineDolly dolly;
    public float dureeTrajet = 2f;

    [Header("Pour la transition finale")]
    public SceneMenuNavigator navigator; // Ton script qui gère les caméras
    public CinemachineVirtualCameraBase Cam; 

    public void LancerLeTrajet()
    {
        if (dolly == null) dolly = GetComponent<CinemachineSplineDolly>();
        StopAllCoroutines();
        StartCoroutine(Trajet());
    }

    private IEnumerator Trajet()
    {
        float temps = 0f;
        dolly.CameraPosition = 0f;

        // On fait avancer la caméra jusqu'en bas
        while (temps < dureeTrajet)
        {
            temps += Time.deltaTime;
            dolly.CameraPosition = Mathf.SmoothStep(0f, 1f, temps / dureeTrajet);
            yield return null;
        }

        // On s'assure d'être exactement à la fin du rail
        dolly.CameraPosition = 1f;

        // 🎯 L'ACTION FINALE : On rebascule sur le menu principal !
        if (navigator != null && Cam != null)
        {
            navigator.SwitchToCamera(Cam);
        }
    }
}