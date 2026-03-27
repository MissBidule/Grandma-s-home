using UnityEngine;

public class CampSelector : MonoBehaviour
{
    [Header("Toutes les skins du menu")]
    [Tooltip("Glisse ici TOUTES les skins (Kids et Demons) de ta scène")]
    public SkinItem[] toutesLesSkins;

    // Fonction à appeler quand on clique sur le bouton "KIDS"
    public void SelectionnerKids()
    {
        AppliquerFiltre(SkinItem.Camp.Kids);
    }

    // Fonction à appeler quand on clique sur le bouton "DEMONS"
    public void SelectionnerDemons()
    {
        AppliquerFiltre(SkinItem.Camp.Demons);
    }

    private void AppliquerFiltre(SkinItem.Camp campChoisi)
    {
        // On passe en revue toutes les skins et on leur donne le camp gagnant
        foreach (SkinItem skin in toutesLesSkins)
        {
            if (skin != null)
            {
                skin.MettreAJourSelection(campChoisi);
            }
        }
    }
}