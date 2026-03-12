using UnityEngine;
using Unity.Cinemachine;
using System.Collections;

public class MoteurSpline : MonoBehaviour
{
    [Header("Composant Dolly")]
    public CinemachineSplineDolly dolly;
    public float dureeTrajet = 2f;

    public void LancerLeTrajet()
    {
        if (dolly == null) dolly = GetComponent<CinemachineSplineDolly>();
        StopAllCoroutines();
        StartCoroutine(AvancerSurLeRail());
    }

    private IEnumerator AvancerSurLeRail()
    {
        float temps = 0f;
        dolly.CameraPosition = 0f;
        while (temps < dureeTrajet)
        {
            temps += Time.deltaTime;
            dolly.CameraPosition = Mathf.SmoothStep(0f, 1f, temps / dureeTrajet);
            yield return null;
        }
        dolly.CameraPosition = 1f;
    }

   
}