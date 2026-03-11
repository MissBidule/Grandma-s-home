using UnityEngine;
using Unity.Cinemachine;

public class SceneMenuNavigator : MonoBehaviour
{
    [Header("Glisse ici tes caméras (Classiques ou Séquentielles)")]
    public CinemachineVirtualCameraBase mainCam;
    public CinemachineVirtualCameraBase optionsCam;
    public CinemachineVirtualCameraBase playCam; // <-- C'est ici que tu mettras ta SeqCam !

    // La fonction accepte maintenant n'importe quelle caméra Cinemachine
    public void SwitchToCamera(CinemachineVirtualCameraBase targetCamera)
    {
        // On remet tout à 10
        if (mainCam != null) mainCam.Priority = 10;
        if (optionsCam != null) optionsCam.Priority = 10;
        if (playCam != null) playCam.Priority = 10;

        // On allume la cible à 20
        if (targetCamera != null) targetCamera.Priority = 20;
    }

    public void GoBackToMain()
    {
        SwitchToCamera(mainCam);
    }
}