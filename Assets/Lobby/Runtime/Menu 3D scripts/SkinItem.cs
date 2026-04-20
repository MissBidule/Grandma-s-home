using UnityEngine;

public class SkinItem : MonoBehaviour
{
    public enum Camp { Child, Ghost } //enum pour les camps sur les skins

    [Header("Configuration")]
    public Camp monCamp; //depend du skin à mettre sur chaque skin

    [Header("Le Matériau Fantôme")]
    public Material materialFantome; //ref mat transparent Mat_Blackout
    private Renderer[] skinRenderers; //stock les renderers des skins pour changer les matériaux
    private Material[][] materiauxOriginaux; //stock les mat originaux pour les remettre quand on switch de camp
    private Material[][] materiauxRemplaces; //stock les mat originaux pour les remettre quand on switch de camp

    private void Awake()
    {
        //recup les renderers et stock les mat originaux
        skinRenderers = GetComponentsInChildren<Renderer>();
        materiauxOriginaux = new Material[skinRenderers.Length][];
        materiauxRemplaces = new Material[skinRenderers.Length][];

        for (int i = 0; i < skinRenderers.Length; i++)
        {
            materiauxOriginaux[i] = new Material[skinRenderers[i].materials.Length];
            materiauxRemplaces[i] = new Material[skinRenderers[i].materials.Length];

            for (int j = 0; j < skinRenderers[i].materials.Length; j++)
            {
                if (skinRenderers[i].materials[j] != null)
                {
                    materiauxOriginaux[i][j] = skinRenderers[i].materials[j];
                    materiauxRemplaces[i][j] = materialFantome;
                }
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
        if (estMonCamp)
        {
            for (int i = 0; i < skinRenderers.Length; i++)
            {
                skinRenderers[i].sharedMaterials = materiauxOriginaux[i];
            }
        }
        else if (materialFantome != null)
        {
            for (int i = 0; i < skinRenderers.Length; i++)
            {
                skinRenderers[i].sharedMaterials = materiauxRemplaces[i];
            }
        }
    }
}