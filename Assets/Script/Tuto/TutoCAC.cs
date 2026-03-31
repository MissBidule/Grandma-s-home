using UnityEngine;

public class TutoCAC : MonoBehaviour
{
    public GameObject deathIcon;

    public void ShowObject()
    {
        if (deathIcon != null)
        {
            deathIcon.SetActive(true);
            Debug.Log("chelou");
        }
    }
}
