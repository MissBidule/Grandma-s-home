using UnityEngine;
using UnityEngine.InputSystem;

public class TutoManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {


        foreach(GameObject obj in FindObjectsByType<GameObject>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if(obj.layer == LayerMask.NameToLayer("Child"))
                {
                    PlayerInput playerInput = obj.GetComponent<PlayerInput>();
                    if (playerInput != null)
                    {
                       playerInput.enabled = false;
                    }
                }
            }



    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
