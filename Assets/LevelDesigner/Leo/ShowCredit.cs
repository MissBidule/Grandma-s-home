using UnityEngine;

public class ShowCredit : MonoBehaviour
{
    [Header("Panels de crédit")]
    public GameObject creditEtudiant;
    public GameObject creditRemerciement;
    public GameObject creditPackage;

    [Header("Panel des boutons")]
    public GameObject creditBouton;
    public GameObject boutonBack;



    private void Start()
    {
    }
    public void seeCredit(GameObject creditPanel)
    {
        creditBouton.SetActive(false);
        creditPanel.SetActive(true);
        boutonBack.SetActive(true);
    }

    public void hideCredit()
    {
        creditBouton.SetActive(false);
        creditEtudiant.SetActive(false);
        creditPackage.SetActive(false);
        creditRemerciement.SetActive(false);
        boutonBack.SetActive(false);
    }
    public void seeOutlineGreen(GameObject boutonVert)
    {
        boutonVert.SetActive(true);

    }
    public void seeButton()
    {
        // Affiche tout les boutons pour afficher les différents crédits
        creditBouton.SetActive(true);
        // Cache les différents textes de crédit 
        creditEtudiant.SetActive(false);
        creditPackage.SetActive(false);
        creditRemerciement.SetActive(false);
        boutonBack.SetActive(false);

    }

}
