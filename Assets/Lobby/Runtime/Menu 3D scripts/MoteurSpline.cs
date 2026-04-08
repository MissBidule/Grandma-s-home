using UnityEngine;
using Unity.Cinemachine;
using System.Collections;

public class MoteurSpline : MonoBehaviour
{
    [Header("La Caméra qui fait le trajet")]
    public CinemachineSplineDolly dolly; //ref du CinemachineSplineDolly attaché à la cam
    public float dureeTrajet = 2f; //var pour régler durée trajet env min 3sec pour que ce soit fluide

    [Header("Pour la transition finale")]
    public SceneMenuNavigator navigator; //ref Menu Manager 
    public CinemachineVirtualCameraBase Cam; //ref cam à activer à la fin du spline

    private CinemachineVirtualCameraBase maCameraSpline; //ref cam spline

    private void Start()
    {
        
        maCameraSpline = GetComponent<CinemachineVirtualCameraBase>();
    }
    //switch à la cam du spline puis lance la coroutine pour faire le trajet
    public void LancerLeTrajet()
    {
        if (dolly == null) dolly = GetComponent<CinemachineSplineDolly>();
        StopAllCoroutines();

        
        if (maCameraSpline != null)
        {
            maCameraSpline.Priority = 30; //met la cam du spline active
        }

        StartCoroutine(Trajet());
    }
    //coroutine trajet qui suit la spline puis switch à la cam finale à la fin
    private IEnumerator Trajet()
    {
        float temps = 0f;
        
        dolly.CameraPosition = 0f;

        while (temps < dureeTrajet)
        {
            temps += Time.deltaTime;
            dolly.CameraPosition = Mathf.SmoothStep(0f, 1f, temps / dureeTrajet); //suit la spline avec une interpolation pour que ce soit fluide
            yield return null;
        }

        dolly.CameraPosition = 1f;

        if (maCameraSpline != null)
        {
            maCameraSpline.Priority = 10;
        }

        if (navigator != null && Cam != null)
        {
            navigator.SwitchToCamera(Cam);
        }
    }
}