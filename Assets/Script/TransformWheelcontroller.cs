using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class TransformWheelcontroller : MonoBehaviour
{
    public Animator anim;
    private bool transformWheelSelected = false;
    public Image selectedTransform;
    public Sprite noImage;
    public static int transformID;

    void Awake()
    {
        if (anim == null)
            anim = GetComponent<Animator>();
    }

    void Update()
    {
        // New Input System: read Tab key
        if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame)
        {
            bool open = !anim.GetBool("OpenTransformWheel");
            anim.SetBool("OpenTransformWheel", open);
        }
        switch (transformID)
        {
            case 0:
                selectedTransform.sprite = noImage;
                break;
            case 1:
                Debug.Log("Chaise/carré");
                break;
            case 2:
                Debug.Log("Armoire/Cylindre");
                break;
            case 3:
                Debug.Log("machin");
                break;
            case 4:
                Debug.Log("bidule");
                break;
            case 5:
                Debug.Log("chose");
                break;
            case 6:
                Debug.Log("objet");
                break;
            case 7:
                Debug.Log("item");
                break;
            case 8:
                Debug.Log("accessoire");
                break;
            case 9:
                Debug.Log("Amoir");
                break;

        }

    }
}
