using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NewMonoBehaviourScript : MonoBehaviour
{
    public float scrollSpeed = 2f;
    private bool isScrolling = false;
    public Vector3 startPosition;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        startPosition= transform.position;

    }
    public void StartScrolling()
    {
        transform.position = startPosition;
        startPosition = transform.position;
        isScrolling = true;
    }


    void Update()
    {
        //if (!isScrolling) return;
        //transform.position += new Vector3(0, 0, scrollSpeed + Time.deltaTime);
    }
}