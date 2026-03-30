using UnityEngine;

public class CampSelector : MonoBehaviour
{
    [Header("Toutes les skins du menu")]
    [Tooltip("tous les skins (kids et ghost)")]
    public SkinItem[] toutesLesSkins;//ref de tous les skins du menu pour les changer quand on switch de camp

    //onclick sur le bouton Kids
    public void SelectionnerKids()
    {
        AppliquerFiltre(SkinItem.Camp.Kids);
    }
    //onclick sur le bouton Ghost
    public void SelectionnerGhost()
    {
        AppliquerFiltre(SkinItem.Camp.Ghost);
    }
    //applique le filtre pour afficher les skins du camp choisi et rendre les autres transparents et non interactifs
    private void AppliquerFiltre(SkinItem.Camp campChoisi)
    {
        foreach (SkinItem skin in toutesLesSkins)
        {
            if (skin != null)
            {
                skin.MettreAJourSelection(campChoisi); //appelle la méthode de chaque skin pour transparence et interaction
            }
        }
    }
}