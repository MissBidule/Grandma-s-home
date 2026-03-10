using UnityEngine;
using PurrNet;
using System.Collections;

public class PredictiveMovement : NetworkBehaviour
{
    private NetworkTransform projection;
    private float frameRate = 1 / 60f;


    private void Start()
    {
        if (isServer) // Imagine Client Side Prediction in Server Side LMAO
        {
            enabled = false;
            return;
        }
        projection = GetComponentInChildren<NetworkTransform>();
        StartCoroutine(PredictiveUpdate());
    }


    private IEnumerator PredictiveUpdate()
    {
        while (true)
        {
            yield return new WaitForSeconds(frameRate);
            Vector3 currentPosition = projection.transform.position;
        }
    }
}