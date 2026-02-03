using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TransformWheelButtonController : MonoBehaviour
{
    public int ID;
    private Animator anim;
    public string transformName;
    public TextMeshProUGUI transformText;
    public Image selectedTransform;
    private bool selected = false;
    public Sprite icon;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        anim = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        if(selected)
        {
            selectedTransform.sprite = icon;
        }

    }

    public void Select()
    {
        selected = true;
        TransformWheelcontroller.transformID = ID;
    }

    public void Deselect()
    {
        selected = false;
        TransformWheelcontroller.transformID = 0;
    }

    public void HoverEnter()
    {
        anim.SetBool("Hover", true);
        transformText.text = transformName;
    }

   public void HoverExit()
    {
        anim.SetBool("Hover", false);
        transformText.text = "";
    }
}
