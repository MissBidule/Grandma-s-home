using UnityEngine;

public class TutoManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        ChildController childController = FindAnyObjectByType<ChildController>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
