using UnityEngine;

public class SkinItem : MonoBehaviour
{
    public enum Camp { Child, Ghost } //enum pour les camps sur les skins

    [Header("Configuration")]
    public Camp monCamp; //depend du skin à mettre sur chaque skin

    [Header("Le Matériau Fantôme")]
    public Material materialFantome; //ref mat transparent Mat_Blackout

    private Renderer[] tousLesRenderers; //recup les renderers
    private Material[] materiauxOriginaux; //stock les mat originaux pour les remettre quand on switch de camp

    private void Awake()
    {
        //recup les renderers et stock les mat originaux
        tousLesRenderers = GetComponentsInChildren<Renderer>(true);
        materiauxOriginaux = new Material[tousLesRenderers.Length];

        for (int i = 0; i < tousLesRenderers.Length; i++)
        {
            if (tousLesRenderers[i] != null)
            {
                materiauxOriginaux[i] = tousLesRenderers[i].material;
            }
        }
    }
    //met à jour la skin en fonction du camp sélectionné
    public void MettreAJourSelection(Camp campSelectionne)
    {
        bool estMonCamp = (monCamp == campSelectionne);
        //change le layer entre 0 default et 2 ignore raycast qui permet de plus pouvoir click sur les colliders
        int layerCible = estMonCamp ? 0 : 2;

        // Retirer les colliders
        Collider[] tousLesColliders = GetComponentsInChildren<Collider>(true);
        foreach (Collider col in tousLesColliders)
        {
            if (col != null)
            {
                col.gameObject.layer = layerCible;
                col.enabled = estMonCamp;
            }
        }
        //changement de matériau pour rendre transparent
        for (int i = 0; i < tousLesRenderers.Length; i++)
        {
            if (tousLesRenderers[i] != null)
            {
                if (estMonCamp)
                {
                    tousLesRenderers[i].material = materiauxOriginaux[i];
                }
                else if (materialFantome != null)
                {
                    tousLesRenderers[i].material = materialFantome;
                }
            }
        }
    }
}