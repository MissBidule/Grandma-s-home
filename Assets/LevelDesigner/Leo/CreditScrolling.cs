using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NewMonoBehaviourScript : MonoBehaviour
{
    public float scrollSpeed = 2f;
    private bool isScrolling = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }
    public void StartScrolling()
    {
        isScrolling = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (!isScrolling) return;
        transform.position += new Vector3(0, 0, scrollSpeed + Time.deltaTime);
    }
}